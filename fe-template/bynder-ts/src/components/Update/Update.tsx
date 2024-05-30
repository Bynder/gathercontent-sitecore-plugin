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
import TableGrid from "../ui/Table/TableGrid";

import { canUseDOM, findItem, getUrlAPI, getUrlVars } from "../../utils/utils";
import usePrevious from "../../utils/hooks";

import {
  formDataRefUpdateType,
  Item,
  Language,
  Project,
  ResultItem,
  Status,
  UpdateData,
} from "./Update.type";

import spinIcon from "../../icons/sc-spinner32.gif";
import rightIcon from "../../icons/Right.png";
import warningIcon from "../../icons/Warning.png";

import "./Update.scss";

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

const Update = () => {
  const preloader = useRef<HTMLDivElement | null>(
    document.querySelector(".preloader")
  );
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [currentMode, setCurrentMode] = useState<number>(
    MODE.ChooseItemsForImport
  );
  const [errorText, setErrorText] = useState<string>("");

  const [data, setData] = useState<UpdateData>();
  const prevData = usePrevious(data);

  const [project, setProject] = useState<Project>();
  const [items, setItems] = useState<Item[]>([]);
  const [language, setLanguage] = useState<string>(
    window.location.host.includes("localhost") ? "en" : getUrlVars()["l"]
  );
  const [languages, setLanguages] = useState<Language[]>([]);
  const [statuses, setStatuses] = useState<Status[]>([]);
  const [statusFilter, setStatusFilter] = useState<Status>();
  const [templates, setTemplates] = useState<Project[]>([]);
  const [templateFilter, setTemplateFilter] = useState<Project>();
  const [projects, setProjects] = useState<Project[]>([]);
  const [projectsFilter, setProjectsFilter] = useState<Project>();

  const [expandContentWorkflowLinks, setExpandContentWorkflowLinks] =
    useState<boolean>(false);

  const [query, setQuery] = useState<string>("");

  const formDataRef = useRef<formDataRefUpdateType>({
    selectedItems: [],
    notUpdatedItemsCount: 0,
    successUpdatedItemsCount: 0,
    resultItems: [],
  });

  // fetch init data
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

    try {
      const response = await fetch(
        `${getUrlAPI()}/api/sitecore/Update/Get?id=${id}&db=${db}`,
        {
          method: "GET",
        }
      );

      if (!response.ok) {
        errorFetchHandle("Network response was not ok.");
      }

      const responseData: UpdateData = await response.json();
      setData(responseData);
    } catch (error: any) {
      errorFetchHandle(error.message);
    }

    setIsLoading(false);
  };

  //update
  const updateItems = async () => {
    const id = window.location.host.includes("localhost")
      ? "{110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}"
      : "{" + getUrlVars()["id"] + "}";
    const lang = language;

    /**
     * in old KO version
     * var status = self.statusFilter();
     * if (!self.statusPostState()) status = "";
     * but checkbox statusPostState commented\disabled
     * so status set  as ''
     */
    const status = "";

    const itemsCollect: { CWBId: string; CMSId: string }[] = [];
    formDataRef.current.selectedItems.forEach(function (item: Item) {
      itemsCollect.push({
        CWBId: item.CwbItem.Id,
        CMSId: item.Id,
      });
    });

    try {
      const response = await fetch(
        `${getUrlAPI()}/api/sitecore/Update/UpdateItems?id=${id}&statusId=${status}&language=${lang}&expandLinks=${expandContentWorkflowLinks}`,
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

      const notUpdatedItemsCount = getNotUpdatedItemsCount(responseData);
      formDataRef.current.notUpdatedItemsCount = notUpdatedItemsCount;
      formDataRef.current.successUpdatedItemsCount =
        responseData.length - notUpdatedItemsCount;
      formDataRef.current.resultItems = responseData;

      buttonClick(MODE.ImportResult);
    } catch (error: any) {
      errorFetchHandle(error.message);
    }
  };

  const getNotUpdatedItemsCount = (items: ResultItem[]) => {
    let count = 0;
    items.forEach(function (item) {
      if (!item.IsImportSuccessful) count++;
    });

    return count;
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
      updateItems();
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

  const closeModal = () => {
    if (canUseDOM() && window?.top?.dialogClose) {
      window.top.dialogClose();
    }
    buttonClick(MODE.ChooseItemsForImport);
  };

  // filters functions
  const filterByName = () => {
    const value = query;
    if (updateTable) {
      const column = updateTable.getColumn("CwbItemName");
      column?.setFilterValue(value);
    }
  };

  const filterByStatus = () => {
    const value = statusFilter;
    if (updateTable) {
      const column = updateTable.getColumn("status");
      column?.setFilterValue(value?.Name);
    }
  };

  const filterByTemplate = () => {
    const value = templateFilter;
    if (updateTable) {
      const column = updateTable.getColumn("CwbTemplateName");
      column?.setFilterValue(value?.Name);
    }
  };

  const filterByProject = () => {
    const value = projectsFilter;
    if (updateTable) {
      const column = updateTable.getColumn("CwbProjectName");
      column?.setFilterValue(value?.Name);
    }
  };

  const filterItems = () => {
    filterByName();
    filterByStatus();
    filterByTemplate();
    filterByProject();
  };

  const getSelectedItemsCount = () => {
    return Object.keys(updateSelectedRow).length;
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

  //update table
  const defaultUpdateColumns = useMemo<ColumnDef<Item>[]>(
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
        accessorKey: "ScTitle",
        id: "ScTitle",
        header: () => <span className="th-name">Sitecore Title</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "CwbItem.Name",
        id: "CwbItemName",
        header: () => (
          <span className="th-name">Content Workflow Item Name</span>
        ),
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "CwbProject.Name",
        id: "CwbProjectName",
        header: () => <span className="th-name">Content Workflow Project</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "LastUpdatedInCwb",
        id: "LastUpdatedInCwb",
        header: () => (
          <span className="th-name">Last updated in Content Workflow</span>
        ),
        cell: (info) => info.getValue(),
        size: 100,
      },
      {
        accessorKey: "LastUpdatedInSitecore",
        id: "LastUpdatedInSitecore",
        header: () => <span className="th-name">Last updated in Sitecore</span>,
        cell: (info) => info.getValue(),
        size: 100,
      },
      {
        accessorKey: "CwbTemplate.Name",
        id: "CwbTemplateName",
        header: () => (
          <span className="th-name">Content Workflow Template</span>
        ),
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "ScTemplateName",
        id: "ScTemplateName",
        header: () => <span className="th-name">Sitecore Template</span>,
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
        size: 70,
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
        size: 70,
      },
    ],
    []
  );

  const [updateColumnOrder, setUpdateColumnOrder] = useState<ColumnOrderState>(
    defaultUpdateColumns.map((column) => column.id as string)
  );
  const [updateSorting, setUpdateSorting] = useState<SortingState>([]);
  const [updateSelectedRow, setUpdateSelectedRow] = useState<RowSelectionState>(
    {}
  );

  const updateTable = useReactTable({
    data: items,
    columns: defaultUpdateColumns,
    state: {
      rowSelection: updateSelectedRow,
      columnOrder: updateColumnOrder,
      sorting: updateSorting,
    },
    enableRowSelection: true,
    onRowSelectionChange: setUpdateSelectedRow,
    onColumnOrderChange: setUpdateColumnOrder,
    getCoreRowModel: getCoreRowModel(),
    onSortingChange: setUpdateSorting,
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
    columnResizeMode: "onChange",
  });

  //confirm  table
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
        accessorKey: "ScTitle",
        id: "ScTitle",
        header: () => <span className="th-name">Sitecore Title</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "CwbItem.Name",
        id: "CwbItemName",
        header: () => (
          <span className="th-name">Content Workflow Item Name</span>
        ),
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "CwbProject.Name",
        id: "CwbProjectName",
        header: () => <span className="th-name">Content Workflow Project</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "LastUpdatedInCwb",
        id: "LastUpdatedInCwb",
        header: () => (
          <span className="th-name">Last updated in Content Workflow</span>
        ),
        cell: (info) => info.getValue(),
        size: 100,
      },
      {
        accessorKey: "LastUpdatedInSitecore",
        id: "LastUpdatedInSitecore",
        header: () => <span className="th-name">Last updated in Sitecore</span>,
        cell: (info) => info.getValue(),
        size: 100,
      },
      {
        accessorKey: "CwbTemplate.Name",
        id: "CwbTemplateName",
        header: () => (
          <span className="th-name">Content Workflow Template</span>
        ),
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "ScTemplateName",
        id: "ScTemplateName",
        header: () => <span className="th-name">Sitecore Template</span>,
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
        size: 70,
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
        size: 70,
      },
    ],
    []
  );

  const [confirmOrder, setConfirmOrder] = useState<ColumnOrderState>(
    defaultConfirmColumns.map((column) => column.id as string)
  );
  const [confirmSorting, setConfirmSorting] = useState<SortingState>([]);

  const confirmTable = useReactTable({
    data: formDataRef.current.selectedItems,
    columns: defaultConfirmColumns,
    state: {
      columnOrder: confirmOrder,
      sorting: confirmSorting,
    },
    onColumnOrderChange: setConfirmOrder,
    getCoreRowModel: getCoreRowModel(),
    onSortingChange: setConfirmSorting,
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
        header: () => <span className="th-name">Update status</span>,
        cell: ({ row, getValue }) => (
          <div style={{ color: getUpdateResultMessageColor(row.original) }}>
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

  const getUpdateResultMessageColor = (item: ResultItem) => {
    if (!item.IsImportSuccessful) return "red";
    return "green";
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
    if (!deepEqual(data, prevData)) {
      formDataRef.current = {
        selectedItems: [],
        notUpdatedItemsCount: 0,
        successUpdatedItemsCount: 0,
        resultItems: [],
      };

      setStatuses(data?.Filters?.Statuses || []);
      setProjects(data?.Filters?.Projects || []);
      setProject(data?.Filters.Project);
      setTemplates(data?.Filters?.Templates || []);
      setLanguages(data?.Languages || []);

      setItems(data?.Items || []);
      filterItems();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data]);

  useEffect(() => {
    const timeout = setTimeout(() => filterItems(), 300);
    return () => {
      clearTimeout(timeout);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query, statusFilter, templateFilter, projectsFilter]);

  useEffect(() => {
    const collectSelectedItems: Item[] = [];
    for (const key in updateSelectedRow) {
      //disable prototype chain
      if (updateSelectedRow.hasOwnProperty(key)) {
        collectSelectedItems.push(items[Number(key)]);
      }
    }
    formDataRef.current.selectedItems = [...collectSelectedItems];
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [updateSelectedRow]);

  return (
    <>
      {/* mode 1 */}
      {getMode(MODE.ChooseItemsForImport) && (
        <div className="content-workflow-dialog">
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
              name="import-project"
              value={project?.Id}
              onChange={(e) =>
                setProjectsFilter(findItem("Id", projects, e.target.value))
              }
            >
              <option value="">CONTENT WORKFLOW PROJECT</option>
              {projects.map((project) => (
                <option key={project.Id} value={project.Id}>
                  {project.Name}
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

          <TableGrid tableType={updateTable}></TableGrid>

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
        <div className="content-workflow-dialog">
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "flex-start",
              marginBottom: "10px",
            }}
          >
            <div className="content-workflow-dialog__note">
              <h2>Confirm update selection</h2>
              <p>
                You're about to overwrite Sitecore content with new content from
                Content Workflow. Please review your selection before updating
                your content.
              </p>
            </div>
            <div className="filter-wrapper filter-wrapper--column">
              <h3 className="filter-wrapper__title">LANGUAGE</h3>
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

          <TableGrid tableType={confirmTable}></TableGrid>

          <div className="pagination pagination--border">
            <span className="pagination__total">
              Total Items: {formDataRef.current.selectedItems.length}
            </span>

            <div className="block" style={{ marginTop: "10px" }}>
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={expandContentWorkflowLinks}
                  onChange={(e) => {
                    setExpandContentWorkflowLinks(e.target.checked);
                  }}
                />

                <b>Expand Content Workflow Links</b>
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
                value="Update >"
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
            <h2 className="import-preloader__title">Update in progress</h2>
            <p>
              Please do not close your browser. Depending on the number of
              items, this can take some time.
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

      {/* mode 5 */}
      {getMode(MODE.ImportResult) && (
        <div className="content-workflow-dialog">
          <div className="content-workflow-dialog__note">
            <h2>Update complete: Results.</h2>
            {formDataRef.current.successUpdatedItemsCount > 0 && (
              <div className="filter-wrapper">
                <img
                  src={rightIcon}
                  width="20"
                  height="20"
                  alt="import success"
                />
                <span>{formDataRef.current.successUpdatedItemsCount}</span>{" "}
                items were updated successfully.
              </div>
            )}

            {formDataRef.current.notUpdatedItemsCount > 0 && (
              <div className="filter-wrapper">
                <img
                  src={warningIcon}
                  width="20"
                  height="20"
                  alt="import error"
                />
                <span>{formDataRef.current.notUpdatedItemsCount}</span> items
                were not updated. Check errors below.
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

                {formDataRef.current.notUpdatedItemsCount > 0 && (
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
                id="mode-5-close"
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

export default Update;
