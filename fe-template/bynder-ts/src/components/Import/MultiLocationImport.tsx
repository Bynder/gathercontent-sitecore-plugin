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
  ExpandedState,
  getExpandedRowModel,
} from "@tanstack/react-table";

import IndeterminateCheckbox from "../ui/Table/IndeterminateCheckbox";
import TreeView from "../ui/TreeView/TreeView";

import {
  canUseDOM,
  findItem,
  getUrlAPI,
  getUrlVars,
  makeStructureData,
  StructureSubrowsData,
} from "../../utils/utils";
import usePrevious from "../../utils/hooks";

import {
  Project,
  ImportData,
  Item,
  Language,
  Status,
  formDataRefMultiLocationImportType,
  ResultItem,
  groupedItem,
  confirmItem,
} from "./Import.type";
import { TreeNodeModification } from "../ui/TreeView/TreeView.type";

import "./Import.scss";

import spinIcon from "../../icons/sc-spinner32.gif";
import rightIcon from "../../icons/Right.png";
import warningIcon from "../../icons/Warning.png";
import TableGrid from "../ui/Table/TableGrid";

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
  Confirm: 3,
  Import: 4,
  ImportResult: 5,
  Close: 6,
  Error: 7,
};

const MultiLocationImport = () => {
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

  const formDataRef = useRef<formDataRefMultiLocationImportType>({
    selectedItems: [],
    notImportedItemsCount: 0,
    successImportedItemsCount: 0,
    resultItems: [],
    confirmItems: [],
    groupedItems: [],
    groupedItemsRestructured: [],
    selectedGroupItems: [],
  });

  const [query, setQuery] = useState<string>("");

  // error handlers
  const errorFetchHandle = (response: string) => {
    if (isLoading) {
      setIsLoading(false);
    }
    setErrorText(`Error: ${response}`);
    buttonClick(MODE.Error);
    throw new Error(response);
  };

  const closeModal = () => {
    if (canUseDOM() && window?.top?.dialogClose) {
      window.top.dialogClose();
    }
    buttonClick(MODE.ChooseItemsForImport);
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
        ChooseDefaultLocation();
        setCurrentMode(newMode);
      }
    } else if (newMode === MODE.Confirm) {
      var emptyLocationError = false;
      if (formDataRef.current.selectedGroupItems.length === 0) {
        setErrorText("Please select at least one item");
      } else {
        formDataRef.current.selectedGroupItems.forEach((item) => {
          if (item.DefaultLocation === "") {
            emptyLocationError = true;
          }
        });
        if (!emptyLocationError) {
          setCurrentMode(newMode);
          switchToCheckItemsBeforeImport();
          setErrorText("");
        } else {
          setErrorText("Please select default location");
        }
      }
    } else if (newMode === MODE.Import) {
      setCurrentMode(newMode);
      importItems();
    } else if (newMode === MODE.Close) {
      setCurrentMode(newMode);
      closeModal();
    } else {
      setCurrentMode(newMode);
    }
  };

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
        `${getUrlAPI()}/api/sitecore/Import/GetMultiLocation??id=${id}&projectId=${projectId}&db=${db}`,
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

  //import
  const importItems = async () => {
    const itemsCollect: {
      Id: string;
      SelectedMappingId: string;
      SelectedLocation: string;
      IsImport: boolean;
    }[] = [];

    formDataRef.current.confirmItems.forEach(function (item: confirmItem, i) {
      itemsCollect.push({
        Id: item.ItemId,
        SelectedLocation: item.DefaultLocation,
        IsImport: true, // TODO redundant?
        SelectedMappingId: item.MappingId,
      });
    });

    const lang = language;
    const statusId = statusPostState ? successfulFilter?.Id : "";
    const projectId = project?.Id;

    try {
      const response = await fetch(
        `${getUrlAPI()}/api/sitecore/Import/ImportItemsWithLocation?projectId=${projectId}&statusId=${statusId}&language=${lang}&expandLinks=${expandContentWorkflowLinks}`,
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

  const getNotImportedItemsCount = (items: ResultItem[]) => {
    let count = 0;
    items.forEach(function (item) {
      if (!item.IsImportSuccessful) count++;
    });

    return count;
  };

  const getSelectedItemsCount = (arr: RowSelectionState) => {
    return Object.keys(arr).length;
  };

  //functionality

  const ChooseDefaultLocation = () => {
    const selectedItems = formDataRef.current.selectedItems;
    const result: groupedItem[] = [];

    selectedItems.forEach((item) => {
      item.AvailableMappings.Mappings.forEach((mapping) => {
        let found = false;
        for (let i = 0; i < result.length; i++) {
          // TODO: maybe need to use if (result[i].Id === mapping.Id)
          if (result[i].MappingName === mapping.Title) {
            found = true;
            break;
          }
        }
        if (!found) {
          result.push({
            Id: mapping.Id,
            TemplateName: item.Template.Name,
            MappingName: mapping.Title,
            ScTemplate: mapping.ScTemplate,
            DefaultLocation: mapping.DefaultLocation,
            DefaultLocationTitle: mapping.DefaultLocationTitle,
            OpenerId: mapping.OpenerId,
          });
        }
      });
    });

    formDataRef.current.groupedItems = [...result];
    formDataRef.current.groupedItemsRestructured = makeStructureData(
      result,
      "TemplateName"
    );
  };

  const switchToCheckItemsBeforeImport = () => {
    var result: any[] = [];
    var items = formDataRef.current.selectedItems;
    formDataRef.current.confirmItems = [];

    items.forEach((item) => {
      const templateItems: any = findByTemplateName(item.Template.Name) || [];

      templateItems.forEach((templateItem: any) => {
        item.AvailableMappings.Mappings.forEach((mapping) => {
          if (mapping.Id === templateItem.Id) {
            result.push({
              ItemId: item.Id,
              ItemTitle: item.Title,
              TemplateName: item.Template.Name,
              MappingId: mapping.Id,
              MappingName: mapping.Title,
              ScTemplate: mapping.ScTemplate,
              DefaultLocation: templateItem.DefaultLocation,
              DefaultLocationTitle: templateItem.DefaultLocationTitle,
            });
          }
        });
      });
    });

    formDataRef.current.confirmItems = [...result];
  };

  const findByTemplateName = (data: any) => {
    const result: any[] = [];
    formDataRef.current.selectedGroupItems.forEach(function (item) {
      if (item.TemplateName === data) {
        result.push(item);
      }
    });
    return result;
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

  //grouped table
  const defaultGroupedColumns = useMemo<ColumnDef<StructureSubrowsData>[]>(
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
        accessorKey: "TemplateName",
        id: "TemplateName",
        header: () => <span className="th-name">Template Name</span>,
        cell: ({ row, getValue }) => {
          return (
            <>
              {row.getCanExpand() ? (
                <button
                  className={`row-group-toggle ${
                    row.getIsExpanded() ? "row-group-toggle--expanded" : ""
                  }`}
                  {...{
                    onClick: row.getToggleExpandedHandler(),
                  }}
                >
                  {row.getIsExpanded() ? (
                    // arrow down
                    <span>&#11206;</span>
                  ) : (
                    // arrow right
                    <span>&#11208;</span>
                  )}
                  {`${getValue()} (${row.subRows.length})`}
                </button>
              ) : (
                getValue()
              )}
            </>
          );
        },
      },
      {
        accessorKey: "MappingName",
        id: "MappingName",
        header: () => <span className="th-name">Mapping Name</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "ScTemplate",
        id: "ScTemplate",
        header: () => <span className="th-name">Sitecore Template</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "DefaultLocationTitle",
        id: "DefaultLocationTitle",
        header: () => <span className="th-name">Default Location</span>,
        cell: ({ row }) => {
          return (
            <>
              {!row.getCanExpand() ? (
                <>
                  <input
                    className="content-workflow-input"
                    placeholder="Default location"
                    name="DefaultLocationTitle"
                    defaultValue={row.original.DefaultLocationTitle}
                    type="text"
                    readOnly={true}
                    autoComplete="off"
                    data-groupid={row.original.groupId || ""}
                    data-openerid={row.original.OpenerId || ""}
                    onClick={openDropTree}
                    style={{
                      position: "absolute",
                      top: "1px",
                      left: "1px",
                      bottom: "2px",
                      right: "2px",
                      paddingTop: "5px",
                      paddingBottom: "5px",
                    }}
                  />
                  <div
                    className="tree-view-container"
                    data-id={row.original.OpenerId}
                  >
                    <TreeView
                      onLocationSelect={handleLocationSelect}
                      className={"d-none"}
                      id={row.original.OpenerId || ""}
                    />
                  </div>
                </>
              ) : (
                false
              )}
            </>
          );
        },
        meta: {
          relativeCell: true,
        },
      },
    ],
    // eslint-disable-next-line react-hooks/exhaustive-deps
    []
  );

  const [groupedOrder, setGroupedOrder] = useState<ColumnOrderState>(
    defaultGroupedColumns.map((column) => column.id as string)
  );
  const [groupedSorting, setGroupedSorting] = useState<SortingState>([]);
  const [groupedExpanded, setGroupedExpanded] = useState<ExpandedState>({});
  const [groupedSelectedRow, setGroupedSelectedRow] =
    useState<RowSelectionState>({});

  const groupedTable = useReactTable({
    data: formDataRef.current.groupedItemsRestructured,
    columns: defaultGroupedColumns,
    state: {
      rowSelection: groupedSelectedRow,
      columnOrder: groupedOrder,
      sorting: groupedSorting,
      expanded: groupedExpanded,
    },
    enableRowSelection: true,
    onRowSelectionChange: setGroupedSelectedRow,
    onColumnOrderChange: setGroupedOrder,
    getCoreRowModel: getCoreRowModel(),
    onSortingChange: setGroupedSorting,
    getSortedRowModel: getSortedRowModel(),
    getSubRows: (row) => row.Subrows,
    onExpandedChange: setGroupedExpanded,
    getExpandedRowModel: getExpandedRowModel(),
    // paginateExpandedRows: false,
    columnResizeMode: "onChange",
  });

  //confirm table
  const defaultConfirmColumns = useMemo<ColumnDef<confirmItem>[]>(
    () => [
      {
        accessorKey: "ItemTitle",
        id: "ItemTitle",
        header: () => <span className="th-name">Title</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "MappingName",
        id: "MappingName",
        header: () => <span className="th-name">Mapping Name</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "ScTemplate",
        id: "ScTemplate",
        header: () => <span className="th-name">Sitecore Template</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "DefaultLocationTitle",
        id: "DefaultLocationTitle",
        header: () => <span className="th-name">Location</span>,
        cell: (info) => info.getValue(),
      },
    ],
    []
  );

  const [confirmOrder, setConfirmOrder] = useState<ColumnOrderState>(
    defaultConfirmColumns.map((column) => column.id as string)
  );
  const [confirmSorting, setConfirmSorting] = useState<SortingState>([]);

  const confirmTable = useReactTable({
    data: formDataRef.current.confirmItems,
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

  //reset

  const resetTablesStates = () => {
    setImportSorting([]);
    setImportSelectedRow({});
    // setSelectedSorting([]);
    // setResultSorting([]);
    setSuccessfulFilter(undefined);
  };

  const getImportResultMessageColor = (item: ResultItem) => {
    if (!item.IsImportSuccessful) return "red";
    return "green";
  };

  //tree
  const groupTreeContainer = useRef<HTMLElement | null>(null);
  const groupTreeInput = useRef<HTMLInputElement | null>(null);

  const handleLocationSelect = (node: TreeNodeModification) => {
    if (groupTreeInput.current) {
      groupTreeInput.current.value = node.data.path;
      const groupId = groupTreeInput.current.dataset.groupid || "";
      formDataRef.current.groupedItemsRestructured[
        Number(groupId[0])
      ].Subrows.forEach((row) => {
        if (row.groupId === groupId) {
          row.DefaultLocationTitle = node.data.path;
          row.DefaultLocation = node.key;
        }
      });
      closeDropTree();
    }
  };

  const openDropTree = (e: React.MouseEvent<HTMLInputElement, MouseEvent>) => {
    e.preventDefault();
    e.stopPropagation();

    if (groupTreeInput.current) {
      if (groupTreeInput.current !== e.target) {
        setTimeout(() => {
          openDropTree(e);
        }, 100);
      }
      closeDropTree();
      return false;
    }

    const clickTarget = e.target as HTMLInputElement;
    groupTreeInput.current = clickTarget;
    const targetId = clickTarget.dataset.openerid || "";
    const targetTree = document.getElementById(targetId);
    if (targetTree) {
      groupTreeContainer.current = targetTree;
      targetTree.classList.remove("d-none");
      targetTree.classList.add("d-block");
    }
  };

  const closeDropTree = (
    e: React.MouseEvent<HTMLElement, MouseEvent> | false = false
  ) => {
    if (e && groupTreeContainer?.current?.contains(e.target as HTMLElement)) {
      return false;
    }
    if (groupTreeContainer.current) {
      groupTreeContainer.current.classList.add("d-none");
      groupTreeContainer.current.classList.remove("d-block");
      groupTreeInput.current = null;
      groupTreeContainer.current = null;
    }
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
    const collectSelectedGroupItems: any[] = [];
    for (const key in groupedSelectedRow) {
      //disable prototype chain
      if (groupedSelectedRow.hasOwnProperty(key)) {
        formDataRef.current.groupedItemsRestructured[
          Number(key[0])
        ].Subrows.forEach((row) => {
          if (row.groupId === key) {
            collectSelectedGroupItems.push(row);
          }
        });
      }
    }
    formDataRef.current.selectedGroupItems = [...collectSelectedGroupItems];
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [groupedSelectedRow]);

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
        selectedItems: [],
        notImportedItemsCount: 0,
        successImportedItemsCount: 0,
        resultItems: [],
        confirmItems: [],
        groupedItems: [],
        groupedItemsRestructured: [],
        selectedGroupItems: [],
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
              Selected Items: {getSelectedItemsCount(importSelectedRow)}
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
          <div className="content-workflow-dialog__note">
            <p>Please review your import selection before importing.</p>
          </div>

          <div className="multi-location-table">
            <TableGrid tableType={groupedTable}></TableGrid>
          </div>

          {/* <div className="tree-view-container" ref={groupTreeContainer}>
            <TreeView
              onLocationSelect={handleLocationSelect}
              className={isLocationTreeShow ? "d-block" : "d-none"}
              id={""}
              style={groupTreePosition.current}
            />
          </div> */}

          <div className="pagination pagination--border">
            <span className="pagination__total">
              Total Items: {formDataRef.current.groupedItems.length}
            </span>
            <span className="pagination__total">
              Selected Items: {getSelectedItemsCount(groupedSelectedRow)}
            </span>
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
                id="mode-2-next"
                type="button"
                value="Next >"
                onClick={() => buttonClick(MODE.Confirm)}
              />

              {errorText && <div className="warning-message">{errorText}</div>}
            </div>

            <div className="help-link">
              <a href="mailto:support@gathercontent.com">Need help?</a>
            </div>
          </footer>
        </div>
      )}

      {/* mode 3 */}
      {getMode(MODE.Confirm) && (
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
              <h2 className="import-preloader__title">
                Confirm import selection
              </h2>
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

          <TableGrid tableType={confirmTable}></TableGrid>

          <div className="pagination pagination--border">
            <span className="pagination__total">
              Total Items: {formDataRef.current.confirmItems.length}
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
                id="mode-3-back"
                type="button"
                value="< Back"
                onClick={() => buttonClick(MODE.CheckItemsBeforeImport)}
              />
              <input
                className="btn"
                id="mode-3-next"
                type="button"
                value="Next >"
                onClick={() => buttonClick(MODE.Import)}
              />

              {errorText && <div className="warning-message">{errorText}</div>}
            </div>

            <div className="help-link">
              <a href="mailto:support@gathercontent.com">Need help?</a>
            </div>
          </footer>
        </div>
      )}

      {/* mode 4 */}
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

      {/* mode 5 */}
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

      {/* mode 7 */}
      {getMode(MODE.Error) && (
        <div className="content-workflow-dialog">
          <div className="content-workflow-dialog__note">
            <h2 className="import-preloader__title">{errorText}</h2>
          </div>
          <footer className="content-workflow-dialog__footer">
            <div className="footer-group">
              <input
                className="btn"
                id="mode-7-close"
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

export default MultiLocationImport;
