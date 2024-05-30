import { useState, useEffect, useRef } from "react";

import { Tree, TreeEventNodeEvent } from "primereact/tree";
import {
  FetchTreeData,
  TreeNodeModification,
  TreeViewProps,
} from "./TreeView.type";

import { getUrlAPI } from "../../../utils/utils";
import "./TreeView.scss";

export default function TreeView(props: TreeViewProps) {
  const {
    onLocationSelect,
    className,
    id,
    style,
    getTopLvlUrl = "/api/sitecore/DropTree/GetTopLevelNode",
    getChildrenLvlUrl = "/api/sitecore/DropTree/GetChildren?id=",
  } = props;
  const [nodes, setNodes] = useState<TreeNodeModification[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  const GetTopLvl = async () => {
    try {
      const response = await fetch(`${getUrlAPI()}${getTopLvlUrl}`, {
        method: "GET",
      });
      const responseData = await response.json();
      const remapData: TreeNodeModification[] = responseData.map(
        (data: FetchTreeData) => {
          return {
            ...data,
            icon: null, //prevent default tree icon
            leaf: false,
            label: data.title,
            id: data.key,
            data: {
              path: data.path,
              iconPath: data.icon, //use custom icon template
            },
          };
        }
      );
      setNodes(remapData);
    } catch (error: any) {
      console.error(`${error?.message}`);
    }
    setLoading(false);
  };

  const loadOnExpand = async (event: TreeEventNodeEvent) => {
    if (!event.node.children?.length) {
      setLoading(true);

      const node: TreeNodeModification = { ...event.node };
      node.children = [];

      try {
        const response = await fetch(
          `${getUrlAPI()}${getChildrenLvlUrl}${node.key}`,
          {
            method: "GET",
          }
        );
        const responseData = await response.json();
        if (responseData.length) {
          const remapData: TreeNodeModification[] = responseData.map(
            (data: FetchTreeData) => {
              return {
                ...data,
                icon: null, //prevent default tree icon
                leaf: false,
                label: data.title,
                id: data.key,
                data: {
                  path: data.path,
                  iconPath: data.icon, //use custom icon template
                },
              };
            }
          );
          node?.children?.push(...remapData);
          let value = [...nodes];
          const findNode = findElementByKey(value, node.key as string);
          if (findNode) {
            findNode.children = node.children;
          }
          setNodes(value);
        }
        setLoading(false);
      } catch (error: any) {
        console.error(`${error?.message}`);
        setLoading(false);
      }
    }
  };

  const onSelect = (event: TreeEventNodeEvent) => {
    onLocationSelect(event.node);
  };

  const findElementByKey = (
    array: TreeNodeModification[],
    key: string
  ): TreeNodeModification | null => {
    for (let i = 0; i < array.length; i++) {
      if (array[i].key === key) {
        return array[i];
      }
      if (array[i].children as TreeNodeModification[]) {
        const found = findElementByKey(
          array[i].children as TreeNodeModification[],
          key
        );
        if (found) {
          return found;
        }
      }
    }
    return null;
  };

  const nodeTemplate = (
    node: TreeNodeModification,
    options: { className: string | undefined }
  ) => {
    // for local test need to add icon in \fe-template\bynder-ts\public\icon
    return (
      <span className={options.className}>
        <img
          src={`${window.location.host.includes("localhost") ? "" : "~"}/icon/${
            node.data.iconPath
          }`}
          alt={node.label}
        />
        {node.label}
      </span>
    );
  };

  const elementRef = useRef(null);
  const topLvlRef = useRef(false);

  useEffect(() => {
    //fetch Top lvl when class change from useState - Import, AddMapping
    if (className === "d-block" && !topLvlRef.current) {
      GetTopLvl();
      topLvlRef.current = true;
    }

    //fetch Top lvl when class change from DOM API (classList.add()) - MultiLocationImport
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (
          mutation.type === "attributes" &&
          mutation.attributeName === "class" &&
          (mutation.target as HTMLElement).classList.contains("d-block") &&
          !topLvlRef.current
        ) {
          GetTopLvl();
          topLvlRef.current = true;
        }
      });
    });

    if (elementRef.current) {
      observer.observe(elementRef.current, { attributes: true });
    }

    return () => {
      observer.disconnect();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [className]);

  return (
    <div
      className={`tree-view-wrapper  ${className || ""}`}
      id={id}
      style={style}
      ref={elementRef}
    >
      {loading && <div className="tree-view-loading-overlay"></div>}
      <div className="custom-scrollbar">
        <Tree
          value={nodes}
          onExpand={loadOnExpand}
          loading={loading}
          selectionMode="single"
          className="tree-view"
          onSelect={onSelect}
          header={null}
          footer={null}
          nodeTemplate={nodeTemplate}
          // collapseIcon={<span>-</span>}
          // expandIcon={<span>+</span>}
        />
      </div>
    </div>
  );
}
