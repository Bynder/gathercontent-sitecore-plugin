import { useEffect, useMemo, useRef, useState } from "react";
import { deepEqual } from "fast-equals";

import {
  ColumnDef,
  ColumnOrderState,
  RowSelectionState,
  SortingState,
  getCoreRowModel,
  getFilteredRowModel,
  getSortedRowModel,
  useReactTable,
} from "@tanstack/react-table";

import IndeterminateCheckbox from "../ui/Table/IndeterminateCheckbox";
import TreeView from "../ui/TreeView/TreeView";
import TableGrid from "../ui/Table/TableGrid";

import { canUseDOM, findItem, getUrlAPI, getUrlVars } from "../../utils/utils";
import usePrevious from "../../utils/hooks";

import {
  Project,
  ImportData,
  Item,
  Language,
  Status,
  formDataRefImportType,
  ResultItem,
} from "./Import.type";
import { TreeNodeModification } from "../ui/TreeView/TreeView.type";

import "./Import.scss";

import spinIcon from "../../icons/sc-spinner32.gif";
import rightIcon from "../../icons/Right.png";
import warningIcon from "../../icons/Warning.png";

declare module "@tanstack/table-core/build/lib/types" {
  // eslint-disable-next-line
  export interface ColumnMeta<TData extends RowData, TValue> {
    relativeCell?: boolean;
    reorder?: boolean;
  }
}

declare global {
  interface Window {
    dialogClose: () => void;
  }
}

const MODE = {
  ChooseItemsForImport: 1,
  CheckItemsBeforeImport: 2,
  Import: 3,
  ImportResult: 4,
  Close: 5,
  Error: 6,
};

const Import = () => {
  const preloader = useRef<HTMLDivElement | null>(
    document.querySelector(".preloader")
  );
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [currentMode, setCurrentMode] = useState<number>(
    MODE.ChooseItemsForImport
  );
  const [errorText, setErrorText] = useState<string>("");

  const [data, setData] = useState<ImportData>();
  const prevData = usePrevious(data);

  const [project, setProject] = useState<Project>();
  const prevProject = usePrevious(project);
  const projectIsChange = useRef(false);
  const [projects, setProjects] = useState<Project[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [language, setLanguage] = useState<string>(
    window.location.host.includes("localhost") ? "en" : getUrlVars()["l"]
  );
  const [languages, setLanguages] = useState<Language[]>([]);
  const [statuses, setStatuses] = useState<Status[]>([]);
  const [statusFilter, setStatusFilter] = useState<Status>();
  const [successfulFilter, setSuccessfulFilter] = useState<Status>();
  const [templates, setTemplates] = useState<Project[]>([]);
  const [templateFilter, setTemplateFilter] = useState<Project>();

  const [statusPostState, setStatusPostState] = useState<boolean>(false);
  const [expandContentWorkflowLinks, setExpandContentWorkflowLinks] =
    useState<boolean>(false);

  const formDataRef = useRef<formDataRefImportType>({
    DefaultLocation: window.location.host.includes("localhost")
      ? "{110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}"
      : "{" + getUrlVars()["id"] + "}",
    DefaultLocationTitle: window.location.host.includes("localhost")
      ? "/sitecore/content/Home"
      : decodeURI(getUrlVars()["t"]),
    selectedItems: [],
    notImportedItemsCount: 0,
    successImportedItemsCount: 0,
    resultItems: [],
  });

  const [query, setQuery] = useState<string>("");
  const getPagedData = async () => {
    setIsLoading(true);
    setErrorText("");
    setItems([]);

    const id = window.location.host.includes("localhost")
      ? "{110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}"
      : "{" + getUrlVars()["id"] + "}";
    const db = window.location.host.includes("localhost")
      ? "master"
      : getUrlVars()["db"];
    const projectId = project?.Id || 0;

    try {
      const response = await fetch(
        `${getUrlAPI()}/api/sitecore/Import/Get?id=${id}&projectId=${projectId}&db=${db}`,
        {
          method: "GET",
        }
      );

      if (!response.ok) {
        errorFetchHandle("Network response was not ok.");
      }

      const responseData: ImportData = await response.json();
      setData(responseData);
      projectIsChange.current = false;
    } catch (error: any) {
      errorFetchHandle(error.message);
    }

    setIsLoading(false);
  };

  const getMode = (section: number) => {
    if (currentMode === section) {
      return true;
    }
    return false;
  };

  const buttonClick = (newMode: number) => {
    if (newMode !== MODE.Error) {
      setErrorText("");
    }

    if (newMode === MODE.CheckItemsBeforeImport) {
      if (formDataRef.current.selectedItems.length === 0) {
        setErrorText("Please select at least one item");
      } else {
        setCurrentMode(newMode);
      }
    } else if (newMode === MODE.Import) {
      setCurrentMode(newMode);
      importItems();
    } else if (newMode === MODE.Close) {
      setCurrentMode(newMode);
      closeModal();
    } else if (newMode === MODE.ChooseItemsForImport) {
      // self.statusFilter = ko.observable();
      setCurrentMode(newMode);
    } else {
      setCurrentMode(newMode);
    }
  };

  //filters functions
  const filterByName = () => {
    const value = query;
    if (importTable) {
      const column = importTable.getColumn("title");
      column?.setFilterValue(value);
    }
  };

  const filterByStatus = () => {
    const value = statusFilter;
    if (importTable) {
      const column = importTable.getColumn("status");
      column?.setFilterValue(value?.Name);
    }
  };

  const filterByTemplate = () => {
    const value = templateFilter;
    if (importTable) {
      const column = importTable.getColumn("template");
      column?.setFilterValue(value?.Name);
    }
  };

  const filterItems = () => {
    filterByName();
    filterByStatus();
    filterByTemplate();
  };

  //import table
  const defaultImportColumns = useMemo<ColumnDef<Item>[]>(
    () => [
      {
        id: "select",
        header: ({ table }) => {
          return (
            <IndeterminateCheckbox
              {...{
                checked: table.getIsAllRowsSelected(),
                indeterminate: table.getIsSomeRowsSelected(),
                onChange: table.getToggleAllRowsSelectedHandler(),
                id: "selectAll",
              }}
            />
          );
        },
        cell: ({ row }) => {
          return (
            <>
              <IndeterminateCheckbox
                {...{
                  checked: row.getIsSelected(),
                  disabled: !row.getCanSelect(),
                  indeterminate: row.getIsSomeSelected(),
                  onChange: row.getToggleSelectedHandler(),
                  id: row.original.Id || row.id,
                }}
              />
              <label
                className="row-seleted-box"
                htmlFor={`${row.original.Id || row.id}`}
              ></label>
            </>
          );
        },
        enableResizing: false,
        size: 40,
        minSize: 40,
        meta: {
          reorder: false,
        },
      },
      {
        accessorKey: "Status.Name",
        id: "status",
        header: () => <span className="th-name">Status</span>,
        cell: ({ row, getValue }) => {
          return (
            <div className="import-status">
              <span
                className="import-status__dot"
                style={{
                  backgroundColor: row?.original?.Status?.Color
                    ? row.original.Status.Color
                    : "",
                }}
              ></span>
              {String(getValue())}
            </div>
          );
        },
      },
      {
        accessorKey: "Title",
        id: "title",
        header: () => <span className="th-name">Item name</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "LastUpdatedInCwb",
        id: "last-updated",
        header: () => (
          <span className="th-name">Last updated in Content Workflow</span>
        ),
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "Breadcrumb",
        id: "path",
        header: () => <span className="th-name">Path</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "Template.Name",
        id: "template",
        header: () => <span className="th-name">Template name</span>,
        cell: (info) => info.getValue(),
        size: 200,
        minSize: 200,
      },
    ],
    []
  );

  const [importColumnOrder, setImportColumnOrder] = useState<ColumnOrderState>(
    defaultImportColumns.map((column) => column.id as string)
  );
  const [importSorting, setImportSorting] = useState<SortingState>([]);
  const [importSelectedRow, setImportSelectedRow] = useState<RowSelectionState>(
    {}
  );

  const importTable = useReactTable({
    data: items,
    columns: defaultImportColumns,
    state: {
      rowSelection: importSelectedRow,
      columnOrder: importColumnOrder,
      sorting: importSorting,
    },
    enableRowSelection: true,
    onRowSelectionChange: setImportSelectedRow,
    onColumnOrderChange: setImportColumnOrder,
    getCoreRowModel: getCoreRowModel(),
    onSortingChange: setImportSorting,
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
    columnResizeMode: "onChange",
  });

  //confirm input table
  const defaultConfirmColumns = useMemo<ColumnDef<Item>[]>(
    () => [
      {
        accessorKey: "Status.Name",
        id: "status",
        header: () => <span className="th-name">Status</span>,
        cell: ({ row, getValue }) => {
          return (
            <div className="import-status">
              <span
                className="import-status__dot"
                style={{
                  backgroundColor: row.original.Status.Color
                    ? row.original.Status.Color
                    : "",
                }}
              ></span>
              {String(getValue())}
            </div>
          );
        },
      },
      {
        accessorKey: "Title",
        id: "title",
        header: () => <span className="th-name">Item name</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "Template.Name",
        id: "template",
        header: () => <span className="th-name">Template name</span>,
        cell: (info) => info.getValue(),
      },
      {
        id: "mappings",
        header: () => <span className="th-name">Specify mappings</span>,
        cell: ({ row }) => {
          return (
            <>
              {row.original.AvailableMappings.Mappings.length <= 1 && (
                <span>{row.original.AvailableMappings.Mappings[0].Title}</span>
              )}
              <select
                name={`mapping-${row.original.Id}`}
                defaultValue={row.original.AvailableMappings.SelectedMappingId}
                className={`content-workflow-input ${
                  row.original.AvailableMappings.Mappings.length > 1
                    ? ""
                    : "d-none"
                }`}
                style={{
                  position: "absolute",
                  top: "1px",
                  left: "1px",
                  bottom: "2px",
                  right: "2px",
                  paddingTop: "5px",
                  paddingBottom: "5px",
                }}
                onChange={(e) => {
                  row.original.AvailableMappings.SelectedMappingId =
                    e.target.value;
                }}
              >
                {row.original.AvailableMappings.Mappings.map((item) => {
                  return (
                    <option value={item.Id} key={item.Id}>
                      {item.Title}
                    </option>
                  );
                })}
              </select>
            </>
          );
        },
        meta: {
          relativeCell: true,
        },
      },
    ],
    []
  );

  const [selectedOrder, setSelectedOrder] = useState<ColumnOrderState>(
    defaultConfirmColumns.map((column) => column.id as string)
  );
  const [selectedSorting, setSelectedSorting] = useState<SortingState>([]);

  const confirmTable = useReactTable({
    data: formDataRef.current.selectedItems,
    columns: defaultConfirmColumns,
    state: {
      columnOrder: selectedOrder,
      sorting: selectedSorting,
    },
    onColumnOrderChange: setSelectedOrder,
    getCoreRowModel: getCoreRowModel(),
    onSortingChange: setSelectedSorting,
    getSortedRowModel: getSortedRowModel(),
    columnResizeMode: "onChange",
  });

  //result table
  const defaultResulColumns = useMemo<ColumnDef<ResultItem>[]>(
    () => [
      {
        accessorKey: "Status.Name",
        id: "status",
        header: () => <span className="th-name">Status</span>,
        cell: ({ row, getValue }) => {
          return (
            <div className="import-status">
              <span
                className="import-status__dot"
                style={{
                  backgroundColor: row.original.Status.Color
                    ? row.original.Status.Color
                    : "",
                }}
              ></span>
              {String(getValue())}
            </div>
          );
        },
      },
      {
        accessorKey: "Title",
        id: "title",
        header: () => <span className="th-name">Item name</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "Message",
        id: "message",
        header: () => <span className="th-name">Import status</span>,
        cell: ({ row, getValue }) => (
          <div style={{ color: getImportResultMessageColor(row.original) }}>
            {String(getValue())}
          </div>
        ),
      },
      {
        accessorKey: "CwbTemplateName",
        id: "template",
        header: () => <span className="th-name">Template name</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "CmsLink",
        id: "openCmsLink",
        header: () => <span className="th-name">Open in Sitecore</span>,
        cell: ({ row, getValue }) => {
          return (
            <>
              {row.original.CmsLink !== null ? (
                <a
                  href={String(getValue())}
                  target="_blank"
                  rel="noreferrer"
                  className="row-btn row-btn--edit"
                >
                  Open
                </a>
              ) : (
                ""
              )}
            </>
          );
        },
      },
      {
        accessorKey: "CwbLink",
        id: "openCwbLink",
        header: () => <span className="th-name">Open in Content Workflow</span>,
        cell: ({ row, getValue }) => {
          return (
            <>
              {row.original.CwbLink !== null ? (
                <a
                  href={String(getValue())}
                  target="_blank"
                  rel="noreferrer"
                  className="row-btn row-btn--edit"
                >
                  Open
                </a>
              ) : (
                ""
              )}
            </>
          );
        },
      },
    ],
    []
  );

  const [resultOrder, setResultOrder] = useState<ColumnOrderState>(
    defaultResulColumns.map((column) => column.id as string)
  );
  const [reusltSorting, setResultSorting] = useState<SortingState>([]);

  const resultTable = useReactTable({
    data: formDataRef.current.resultItems,
    columns: defaultResulColumns,
    state: {
      columnOrder: resultOrder,
      sorting: reusltSorting,
    },
    onColumnOrderChange: setResultOrder,
    getCoreRowModel: getCoreRowModel(),
    onSortingChange: setResultSorting,
    getSortedRowModel: getSortedRowModel(),
    columnResizeMode: "onChange",
  });

  const resetTablesStates = () => {
    setImportSorting([]);
    setImportSelectedRow({});
    setSelectedSorting([]);
    setResultSorting([]);
    setSuccessfulFilter(undefined);
  };

  const getImportResultMessageColor = (item: ResultItem) => {
    if (!item.IsImportSuccessful) return "red";
    return "green";
  };

  //tree
  const importTreeContainer = useRef<HTMLDivElement>(null);
  const [isLocationTreeShow, setIsLocationTreeShow] = useState<boolean>(false);
  const handleLocationSelect = (node: TreeNodeModification) => {
    formDataRef.current.DefaultLocation = node.key as string;
    formDataRef.current.DefaultLocationTitle = node.data.path as string;
    setIsLocationTreeShow(false);
  };

  const openDropTree = (e: React.MouseEvent<HTMLInputElement, MouseEvent>) => {
    e.preventDefault();
    e.stopPropagation();
    if (!importTreeContainer?.current?.contains(e.target as HTMLElement)) {
      setIsLocationTreeShow(!isLocationTreeShow);
    }
  };

  const closeDropTree = (e: React.MouseEvent<HTMLInputElement, MouseEvent>) => {
    if (
      !importTreeContainer?.current?.contains(e.target as HTMLElement) &&
      isLocationTreeShow
    ) {
      setIsLocationTreeShow(false);
    }
  };

  //import
  const importItems = async () => {
    const id = formDataRef.current.DefaultLocation;

    const itemsCollect: { Id: string; SelectedMappingId: string }[] = [];

    formDataRef.current.selectedItems.forEach(function (item: Item, i) {
      if (item.AvailableMappings.SelectedMappingId == null) {
        item.AvailableMappings.SelectedMappingId =
          item.AvailableMappings.Mappings[0].Id;
      }

      itemsCollect.push({
        Id: item.Id,
        SelectedMappingId: item.AvailableMappings.SelectedMappingId,
      });
    });

    const lang = language;
    const statusId = statusPostState ? successfulFilter?.Id : "";
    const projectId = project?.Id;

    try {
      const response = await fetch(
        `${getUrlAPI()}/api/sitecore/Import/ImportItems?id=${id}&projectId=${projectId}&statusId=${statusId}&language=${lang}&expandLinks=${expandContentWorkflowLinks}`,
        {
          method: "POST",
          headers: {
            "Content-Type": "text/plain; charset=utf-8",
          },
          body: JSON.stringify(itemsCollect),
        }
      );
      if (!response.ok) {
        errorFetchHandle("Network response was not ok.");
      }
      const responseData = await response.json();

      if (responseData.status === "error") {
        errorFetchHandle(responseData.message);
      }

      const notImportedItemsCount = getNotImportedItemsCount(responseData);
      formDataRef.current.notImportedItemsCount = notImportedItemsCount;
      formDataRef.current.successImportedItemsCount =
        responseData.length - notImportedItemsCount;
      formDataRef.current.resultItems = responseData;

      buttonClick(MODE.ImportResult);
    } catch (error: any) {
      errorFetchHandle(error.message);
    }
  };

  const getNotImportedItemsCount = (items: ResultItem[]) => {
    let count = 0;
    items.forEach(function (item) {
      if (!item.IsImportSuccessful) count++;
    });

    return count;
  };

  const getSelectedItemsCount = () => {
    return Object.keys(importSelectedRow).length;
  };

  const closeModal = () => {
    if (canUseDOM() && window?.top?.dialogClose) {
      window.top.dialogClose();
    }
    buttonClick(MODE.ChooseItemsForImport);
  };

  // error handlers
  const errorFetchHandle = (response: string) => {
    if (isLoading) {
      setIsLoading(false);
    }
    setErrorText(response);
    buttonClick(MODE.Error);
    throw new Error(response);
  };

  //effects

  useEffect(() => {
    getPagedData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (canUseDOM() && preloader.current) {
      if (isLoading) {
        preloader.current.style.display = "block";
      } else {
        preloader.current.style.display = "none";
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoading]);

  useEffect(() => {
    if (!deepEqual(project, prevProject)) {
      projectIsChange.current = true;
      setStatusFilter(undefined);
      setTemplateFilter(undefined);
      setQuery("");
      resetTablesStates();
      getPagedData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [project]);

  useEffect(() => {
    const collectSelectedItems: Item[] = [];
    for (const key in importSelectedRow) {
      //disable prototype chain
      if (importSelectedRow.hasOwnProperty(key)) {
        collectSelectedItems.push(items[Number(key)]);
      }
    }
    formDataRef.current.selectedItems = [...collectSelectedItems];
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [importSelectedRow]);

  useEffect(() => {
    // prevent Hook operation when the project is changed and filters are reset
    if (!projectIsChange.current) {
      const timeout = setTimeout(() => filterItems(), 300);
      return () => {
        clearTimeout(timeout);
      };
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query, statusFilter, templateFilter]);

  useEffect(() => {
    if (!deepEqual(data, prevData)) {
      formDataRef.current = {
        DefaultLocation: window.location.host.includes("localhost")
          ? "{110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}"
          : "{" + getUrlVars()["id"] + "}",
        DefaultLocationTitle: window.location.host.includes("localhost")
          ? "/sitecore/content/Home"
          : decodeURI(getUrlVars()["t"]),
        selectedItems: [],
        notImportedItemsCount: 0,
        successImportedItemsCount: 0,
        resultItems: [],
      };

      setProjects(data?.Filters?.Projects || []);
      setProject(data?.Filters.Project);
      setStatuses(data?.Filters?.Statuses || []);
      setSuccessfulFilter(data?.Filters.Statuses[0]);
      setTemplates(data?.Filters?.Templates || []);
      setLanguages(data?.Languages || []);

      setItems(data?.Items || []);
      filterItems();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data]);

  return (
    <>
      {/* mode 1 */}
      {getMode(MODE.ChooseItemsForImport) && (
        <div className="content-workflow-dialog">
          <div className="filter-wrapper">
            <h3 className="filter-wrapper__title">CONTENT WORKFLOW PROJECT</h3>
            <select
              className="custom_select content-workflow-input"
              name="import-project"
              value={project?.Id}
              onChange={(e) =>
                setProject(findItem("Id", projects, e.target.value))
              }
            >
              {projects.map((project, index) => (
                <option key={project.Id} value={project.Id}>
                  {index === 0
                    ? project.Name.toLocaleUpperCase()
                    : project.Name}
                </option>
              ))}
            </select>
          </div>
          <div className="filter-wrapper">
            <h3 className="filter-wrapper__title">FILTER</h3>
            <select
              className="custom_select content-workflow-input"
              name="import-statuses"
              value={statusFilter?.Id}
              onChange={(e) => {
                setStatusFilter(findItem("Id", statuses, e.target.value));
              }}
            >
              <option value="">STATUS</option>
              {statuses.map((status) => (
                <option key={status.Id} value={status.Id}>
                  {status.Name}
                </option>
              ))}
            </select>
            <select
              className="custom_select content-workflow-input"
              style={{ marginRight: "auto" }}
              name="import-templates"
              value={templateFilter?.Id}
              onChange={(e) => {
                setTemplateFilter(findItem("Id", templates, e.target.value));
              }}
            >
              <option value="">CONTENT WORKFLOW TEMPLATE</option>
              {templates.map((template) => (
                <option key={template.Id} value={template.Id}>
                  {template.Name}
                </option>
              ))}
            </select>
            <input
              placeholder="FILTER BY ITEM NAME (SEARCH)"
              type="text"
              name="search-query"
              autoComplete="off"
              autoFocus
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="content-workflow-input"
            />
          </div>

          <TableGrid tableType={importTable}></TableGrid>

          <div className="pagination pagination--border">
            <span className="pagination__total">
              Total Items: {items.length}
            </span>
            <span className="pagination__total">
              Selected Items: {getSelectedItemsCount()}
            </span>
          </div>

          <footer className="content-workflow-dialog__footer">
            <div className="footer-group">
              <input
                className="btn"
                id="mode-1-next"
                type="button"
                value="Next >"
                onClick={() => buttonClick(MODE.CheckItemsBeforeImport)}
              />
              {errorText && <div className="warning-message">{errorText}</div>}
            </div>
            <div className="help-link">
              <a href="mailto:support@gathercontent.com">Need help?</a>
            </div>
          </footer>
        </div>
      )}

      {/* mode 2 */}
      {getMode(MODE.CheckItemsBeforeImport) && (
        <div className="content-workflow-dialog" onClick={closeDropTree}>
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "flex-start",
              marginBottom: "10px",
            }}
          >
            <div className="content-workflow-dialog__note">
              <h2>Confirm import selection</h2>
              <p>Please review your import selection before importing.</p>
            </div>
            <div className="filter-wrapper filter-wrapper--column">
              <h3 className="filter-wrapper__title">FILTER</h3>
              <select
                name="language"
                className="content-workflow-input"
                value={language}
                onChange={(e) => {
                  setLanguage(e.target.value);
                }}
              >
                {languages.map((language) => (
                  <option key={language.IsoCode} value={language.IsoCode}>
                    {language.Name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="filter-wrapper">
            <h3 className="filter-wrapper__title">FILTER</h3>

            <div className="tree-view block">
              <input
                className="block content-workflow-input"
                placeholder="Default location"
                name="DefaultLocationTitle"
                value={formDataRef?.current?.DefaultLocationTitle}
                type="text"
                readOnly={true}
                autoComplete="off"
                onClick={openDropTree}
              />
              <div className="tree-view-container" ref={importTreeContainer}>
                <TreeView
                  onLocationSelect={handleLocationSelect}
                  className={isLocationTreeShow ? "d-block" : "d-none"}
                />
              </div>
            </div>
          </div>

          <TableGrid tableType={confirmTable}></TableGrid>

          <div className="pagination pagination--border">
            <span className="pagination__total">
              Total Items: {formDataRef.current.selectedItems.length}
            </span>

            <div className="block" style={{ marginTop: "10px" }}>
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={statusPostState}
                  onChange={(e) => {
                    setStatusPostState(e.target.checked);
                  }}
                />

                <span>
                  After successful import change status of item to
                  <select
                    style={{ margin: "0 5px" }}
                    className="custom_select content-workflow-input"
                    name="import-statuses-confirm"
                    value={successfulFilter?.Id}
                    onChange={(e) => {
                      setSuccessfulFilter(
                        findItem("Id", statuses, e.target.value)
                      );
                    }}
                  >
                    {statuses.map((status) => (
                      <option key={status.Id} value={status.Id}>
                        {status.Name}
                      </option>
                    ))}
                  </select>
                  inside your Content Workflow account.
                </span>
              </label>
              <br />
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={expandContentWorkflowLinks}
                  onChange={(e) => {
                    setExpandContentWorkflowLinks(e.target.checked);
                  }}
                />

                <b> Expand Content Workflow Links</b>
              </label>
            </div>
          </div>

          <footer className="content-workflow-dialog__footer">
            <div className="footer-group">
              <input
                className="btn"
                id="mode-2-back"
                type="button"
                value="< Back"
                onClick={() => buttonClick(MODE.ChooseItemsForImport)}
              />
              <input
                className="btn"
                id="mode-2-import"
                type="button"
                value="Import"
                onClick={() => buttonClick(MODE.Import)}
              />
            </div>

            <div className="help-link">
              <a href="mailto:support@gathercontent.com">Need help?</a>
            </div>
          </footer>
        </div>
      )}

      {/* mode 3 */}
      {getMode(MODE.Import) && (
        <div className="content-workflow-dialog">
          <div className="import-preloader">
            <h2 className="import-preloader__title">Import in progress</h2>
            <p>
              Please do not close your browser. Depending on the number of Items
              you have selected the import can take some time.
            </p>
            <img
              src={spinIcon}
              alt=""
              width={32}
              height={32}
              style={{ marginTop: "20px" }}
            />
          </div>
        </div>
      )}

      {/* mode 4 */}
      {getMode(MODE.ImportResult) && (
        <div className="content-workflow-dialog">
          <div className="content-workflow-dialog__note">
            <h2>Import complete: Results.</h2>
            {formDataRef.current.successImportedItemsCount > 0 && (
              <div className="filter-wrapper">
                <img
                  src={rightIcon}
                  width="20"
                  height="20"
                  alt="import success"
                />
                <span>{formDataRef.current.successImportedItemsCount}</span>{" "}
                items were imported successfully.
              </div>
            )}

            {formDataRef.current.notImportedItemsCount > 0 && (
              <div className="filter-wrapper">
                <img
                  src={warningIcon}
                  width="20"
                  height="20"
                  alt="import error"
                />
                <span>{formDataRef.current.notImportedItemsCount}</span> items
                were not imported. Check errors below.
              </div>
            )}
          </div>

          {formDataRef.current.resultItems.length > 0 && (
            <>
              <TableGrid tableType={resultTable}></TableGrid>

              <div className="pagination pagination--border">
                <span className="pagination__total">
                  Total Items: {formDataRef.current.resultItems.length}
                </span>

                {formDataRef.current.notImportedItemsCount > 0 && (
                  <div className="block" style={{ marginTop: "10px" }}>
                    <strong>Template not mapped:</strong> Please check template
                    mapping.
                    <br />
                    <strong>Template fields mismatch:</strong> This could be
                    because the template in Content Workflow has changed and the
                    changed template has not been updated on Sitecore.
                  </div>
                )}
              </div>
            </>
          )}

          <footer className="content-workflow-dialog__footer">
            <div className="footer-group">
              <input
                className="btn"
                id="mode-4-close"
                type="button"
                value="Close"
                onClick={() => buttonClick(MODE.Close)}
              />
            </div>

            <div className="help-link">
              <a href="mailto:support@gathercontent.com">Need help?</a>
            </div>
          </footer>
        </div>
      )}

      {/* mode 6 */}
      {getMode(MODE.Error) && (
        <div className="content-workflow-dialog">
          <div className="content-workflow-dialog__note">
            <h2 className="import-preloader__title">{errorText}</h2>
          </div>
          <footer className="content-workflow-dialog__footer">
            <div className="footer-group">
              <input
                className="btn"
                id="mode-6-close"
                type="button"
                value="Close"
                onClick={() => buttonClick(MODE.Close)}
              />
            </div>

            <div className="help-link">
              <a href="mailto:support@gathercontent.com">Need help?</a>
            </div>
          </footer>
        </div>
      )}
    </>
  );
};

export default Import;
