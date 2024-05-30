export type MappingItem = {
  IsRelated: boolean;
  MappingGroupId: string;
  CwbProjectName: string;
  CwbTemplateId: string;
  CwbTemplateName: string;
  ScTemplateName: string;
  MappingTitle: string;
  LastMappedDateTime: string;
  LastUpdatedDate: string;
  ScMappingId: string;
  IsMapped: boolean;
  RemovedFromCwb: boolean;
  IsHighlightingDate: boolean;
  // Subrows?: MappingItem[];
  id: string;
  groupId: string;
  parentId: string;
} & EditMapping;

type EditMapping = {
  Manage?: boolean;
  Delete?: boolean;
  className?: string;
};
