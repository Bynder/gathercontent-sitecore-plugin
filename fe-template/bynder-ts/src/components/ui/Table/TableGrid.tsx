import { flexRender } from "@tanstack/react-table";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import DraggableColumnHeader from "./DraggableColumnHeader";
import { ReactNode } from "react";

type TableProps = {
  tableType: any;
  children?: ReactNode;
};

const TableGrid = ({ tableType, children }: TableProps) => {
  return (
    <DndProvider backend={HTML5Backend}>
      <div className="table-wrapper">
        <table>
          <thead>
            {tableType.getHeaderGroups().map((headerGroup: any) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header: any) => (
                  <DraggableColumnHeader
                    key={header.id}
                    header={header}
                    table={tableType}
                  />
                ))}
              </tr>
            ))}
          </thead>

          <tbody>
            {tableType.getRowModel().rows.map((row: any) => {
              return (
                <tr
                  key={row.id}
                  className={row.getIsSelected() ? "row-selected" : ""}
                >
                  {row.getVisibleCells().map((cell: any) => {
                    return (
                      <td
                        key={cell.id}
                        className={`cell-${cell.column.id} ${
                          cell.getIsGrouped()
                            ? "cell-grouped"
                            : cell.getIsAggregated()
                            ? "cell-aggregated"
                            : cell.getIsPlaceholder()
                            ? "cell-placeholder"
                            : "cell-comon"
                        } ${
                          cell.column.columnDef?.meta?.relativeCell
                            ? "cell-relative"
                            : ""
                        }`}
                      >
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext()
                        )}
                      </td>
                    );
                  })}
                </tr>
              );
            })}
          </tbody>
        </table>
        {children}
      </div>
    </DndProvider>
  );
};

export default TableGrid;
