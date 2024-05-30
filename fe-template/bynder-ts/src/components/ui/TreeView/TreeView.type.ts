import { TreeNode } from "primereact/treenode";
import { HTMLAttributes } from "react";

export type TreeViewProps = {
  onLocationSelect: (node: TreeNodeModification) => void;
  id?: string;
  getTopLvlUrl?: string;
  getChildrenLvlUrl?: string;
} & Pick<HTMLAttributes<HTMLDivElement>, "className" | "style">;

export type TreeNodeModification = TreeNode & {
  data?: {
    path?: string;
    iconPath?: string;
  };
};

export type FetchTreeData = {
  title: string;
  key: string;
  children: [];
  isLazy: boolean;
  icon: string;
  select: boolean;
  expand: boolean;
  path: string;
};
