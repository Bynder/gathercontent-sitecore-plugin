import {
  ColumnOrderState,
  Header,
  Table,
  Column,
  flexRender,
} from "@tanstack/react-table";
import { useDrop, useDrag } from "react-dnd";

const reorderColumn = (
  draggedColumnId: string,
  targetColumnId: string,
  columnOrder: string[]
): ColumnOrderState => {
  columnOrder.splice(
    columnOrder.indexOf(targetColumnId),
    0,
    columnOrder.splice(columnOrder.indexOf(draggedColumnId), 1)[0] as string
  );
  return [...columnOrder];
};

const DraggableColumnHeader = ({
  header,
  table,
}: {
  header: Header<any, unknown>;
  table: Table<any>;
}) => {
  const { getState, setColumnOrder } = table;
  const { columnOrder } = getState();
  const { column } = header;

  const [, dropRef] = useDrop({
    accept: "column",
    drop: (draggedColumn: Column<any>) => {
      const newColumnOrder = reorderColumn(
        draggedColumn.id,
        column.id,
        columnOrder
      );
      setColumnOrder(newColumnOrder);
    },
  });

  const [{ isDragging }, dragRef, previewRef] = useDrag({
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
    item: () => column,
    type: "column",
  });

  return (
    <>
      <th
        id={header.id}
        ref={dropRef}
        style={{ width: header.getSize() }}
        className={`${isDragging ? "th-gragging" : ""} ${
          header.column.columnDef?.meta?.reorder === false ? "select-none" : ""
        }`}
      >
        {header.column.columnDef.minSize !== 0 && (
          <>
            <div
              ref={previewRef}
              {...{}}
              className={`th-content ${
                header.column.getCanSort() ? "cursor-pointer select-none" : ""
              }`}
              onClick={header.column.getToggleSortingHandler()}
            >
              {header.isPlaceholder
                ? null
                : flexRender(
                    header.column.columnDef.header,
                    header.getContext()
                  )}

              {{
                asc: <span className="sort-icon"> &#11205;</span>, // arrow down
                desc: <span className="sort-icon">&#11206;</span>, // arrow up
              }[header.column.getIsSorted() as string] ?? null}
              {header.column.columnDef?.meta?.reorder !== false && (
                <button ref={dragRef} className="draggable-button"></button>
              )}
            </div>
            {header.column.getCanResize() && (
              <div
                {...{
                  onDoubleClick: () => header.column.resetSize(),
                  onMouseDown: header.getResizeHandler(),
                  onTouchStart: header.getResizeHandler(),
                  className: `resizer ${
                    header.column.getIsResizing() ? "isResizing" : ""
                  }`,
                }}
              />
            )}
          </>
        )}
      </th>
    </>
  );
};

export default DraggableColumnHeader;
