import { useState, useEffect, useMemo } from "react";
import {
  useReactTable,
  getCoreRowModel,
  ColumnOrderState,
  ColumnDef,
  getSortedRowModel,
  SortingState,
  getPaginationRowModel,
  getExpandedRowModel,
} from "@tanstack/react-table";

import TableGrid from "../ui/Table/TableGrid";

import { MappingItem } from "./Mappings.type";

import { canUseDOM, getUrlAPI } from "../../utils/utils";

import "./Mappings.scss";

import deleteIcon from "../../icons/delete.png";

declare global {
  interface Window {
    scForm: any;
  }
}

const Mappings = () => {
  const [errorText, setErrorText] = useState<string>("");
  const [mappings, setMappings] = useState<MappingItem[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      try {
        const response = await fetch(
          `${getUrlAPI()}/api/sitecore/mappings/Get`,
          {
            method: "GET",
          }
        );
        const responseData = await response.json();
        // setMappings([...makeStructureData(responseData, "MappingTitle")]);
        setMappings([...responseData]);
      } catch (error: any) {
        console.error(`Download error: ${error?.message}`);
        setErrorText(`Download error: ${error?.message}`);
      }
      setIsLoading(false);
      if (canUseDOM() && !isLoading) {
        const preloader: HTMLDivElement | null =
          document.querySelector(".preloader");
        if (preloader) {
          preloader.style.display = "none";
        }
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const setInfoMessage = () => {
    if (mappings?.length > 0) {
      return (
        <p className="content-workflow-dialog__title">
          Choose which templates you wish to manage.
        </p>
      );
    }

    if (mappings?.length === 0) {
      return (
        <p className="content-workflow-dialog__title">
          There are no mapping defined. Please click Add Mappings to start.
        </p>
      );
    }
  };

  const defaultColumns = useMemo<ColumnDef<MappingItem>[]>(
    () => [
      {
        accessorKey: "CwbProjectName",
        id: "CwbProjectName",
        header: () => <span className="th-name">Content Workflow Project</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "CwbTemplateName",
        id: "CwbTemplateName",
        header: () => (
          <span className="th-name">Content Workflow template</span>
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
        accessorKey: "MappingTitle",
        id: "MappingTitle",
        header: ({ table }) => <span className="th-name">Mapping Name</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "LastMappedDateTime",
        id: "LastMappedDateTime",
        header: () => <span className="th-name">Last mapped</span>,
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "LastUpdatedDate",
        id: "LastUpdatedDate",
        header: () => (
          <span className="th-name">Last updated in Content Workflow</span>
        ),
        cell: (info) => info.getValue(),
      },
      {
        accessorKey: "Manage",
        id: "Manage",
        header: "",
        cell: ({ row }) => {
          return (
            <>
              {!row.getCanExpand() ? (
                <button
                  className={`row-btn row-btn--edit ${
                    window?.scForm ? "scForm" : "disable-scForm"
                  }`}
                  onClick={(e) => {
                    e.preventDefault();
                    const rowItem = row.original as MappingItem;
                    const id = rowItem.CwbTemplateId;
                    const scMappingId = rowItem.ScMappingId;

                    window?.scForm &&
                      window?.scForm.showModalDialog(
                        "/sitecore modules/shell/contentworkflow/Mappings/AddOrUpdateMapping.html?id=" +
                          id +
                          "&scMappingId=" +
                          scMappingId,
                        null,
                        "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:600;dialogMinWidth:800;dialogWidth:800;dialogHeight:800;header: Manage Field Mappings"
                      );
                  }}
                >
                  Edit
                </button>
              ) : (
                false
              )}
            </>
          );
        },
        enableResizing: false,
        size: 50,
      },
      {
        accessorKey: "Delete",
        id: "Delete",
        header: "",
        cell: ({ row, table }) => {
          return (
            <>
              {!row.getCanExpand() ? (
                <button
                  className={`row-btn row-btn--delete`}
                  onClick={(e) => {
                    e.preventDefault();
                    const confirmDelete = window.confirm(
                      "Are you sure you want to delete this?"
                    );

                    if (confirmDelete) {
                      const rowItem = row.original as MappingItem;
                      const scMappingId = rowItem.ScMappingId;
                      const newData = [...mappings];

                      newData.forEach((rowData, i) => {
                        if (rowData.ScMappingId === scMappingId) {
                          newData.splice(i, 1);
                        }
                      });

                      (async () => {
                        try {
                          const response = await fetch(
                            `${getUrlAPI()}/api/sitecore/mappings/Delete?scMappingId=${scMappingId}`,
                            { method: "DELETE" }
                          );
                          if (response.ok) {
                            setMappings([...newData]);
                          } else {
                            setErrorText(
                              `Delete error: ${response.url} ${response.status} (${response.statusText})`
                            );
                          }
                        } catch (error: any) {
                          setErrorText(`Delete error: ${error?.message}`);
                        }
                      })();
                    }
                  }}
                >
                  <img src={deleteIcon} alt="delete row" />
                </button>
              ) : (
                false
              )}
            </>
          );
        },
        enableResizing: false,
        size: 50,
      },
    ],
    [mappings]
  );

  const [columnOrder, setColumnOrder] = useState<ColumnOrderState>(
    defaultColumns.map((column) => column.id as string) //must start out with populated columnOrder so we can splice
  );
  const [sorting, setSorting] = useState<SortingState>([]);

  const table = useReactTable({
    data: mappings,
    columns: defaultColumns,
    state: {
      columnOrder,
      sorting,
    },
    onColumnOrderChange: setColumnOrder,
    getCoreRowModel: getCoreRowModel(),
    onSortingChange: setSorting,
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
    paginateExpandedRows: false,
    columnResizeMode: "onChange",
  });

  return (
    <>
      <div className="content-workflow-dialog">
        {errorText && <h1>{errorText}</h1>}
        {setInfoMessage()}

        <TableGrid tableType={table}></TableGrid>

        <div className="pagination pagination--border">
          <span className="pagination__total">
            Total Items: {mappings.length}
          </span>

          <div className="pagination__action">
            <span className="pagination__size">
              Page Size:
              <select
                className="content-workflow-input"
                name="page-size"
                value={table.getState().pagination.pageSize}
                onChange={(e) => {
                  table.setPageSize(Number(e.target.value));
                }}
              >
                {[10, 20, 30, 40, 50].map((pageSize) => (
                  <option key={pageSize} value={pageSize}>
                    {pageSize}
                  </option>
                ))}
              </select>
            </span>

            <div className="pagination__nav">
              <button
                className="btn"
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage()}
              >
                &#11207;
              </button>

              <span className="flex items-center gap-1">
                <strong>
                  {table.getState().pagination.pageIndex + 1}
                  {/* {" "}of{" "}
            {table.getPageCount()} */}
                </strong>
              </span>

              <button
                className="btn"
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage()}
              >
                &#11208;
              </button>
            </div>
          </div>
        </div>

        <footer className="content-workflow-dialog__footer">
          <input
            className="btn"
            style={{ width: "160px" }}
            id="AddMore"
            type="button"
            value="Add Mapping"
            onClick={() => {
              window?.scForm &&
                window?.scForm.showModalDialog(
                  "/sitecore modules/shell/contentworkflow/mappings/AddOrUpdateMapping.html",
                  null,
                  "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:600;dialogMinWidth:800;dialogWidth:800;dialogHeight:800;header: Manage Field Mappings"
                );
            }}
          />
          {/* <input
            className="mappings-btn"
            style={{ width: '160px' }}
            id="AddRelatedMore"
            type="button"
            value="Add Related Mapping"
            data-bind="click: addMoreRelatedTemplates"
          /> */}
        </footer>
      </div>
    </>
  );
};

export default Mappings;
