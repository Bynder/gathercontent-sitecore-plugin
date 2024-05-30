import { StructureSubrowsData } from "../../utils/utils";

export type ImportData = {
  Filters: Filters;
  Items: Item[];
  Languages: Language[];
};

export type Filters = {
  Project: Project;
  Projects: Project[];
  Templates: Project[];
  Statuses: Status[];
};

export type Language = {
  Name: string;
  IsoCode: string;
};

export type Project = {
  Id: string;
  Name: string;
};

export type Item = {
  Id: string;
  Status: Status;
  Title: string;
  LastUpdatedInCwb: string;
  Breadcrumb: string;
  Template: Project;
  AvailableMappings: AvailableMappings;
  __kg_selected__: boolean;
};

export type AvailableMappings = {
  SelectedMappingId?: any | string;
  Mappings: Mapping[];
};

export type Mapping = {
  Id: string;
  Title: string;
  ScTemplate?: any;
  OpenerId?: any;
  DefaultLocation?: any;
  DefaultLocationTitle?: any;
};

export type Status = {
  Id: string;
  Name: string;
  Color?: string | null;
};

export type formDataRefImportType = {
  DefaultLocation: string;
  DefaultLocationTitle: string;
  selectedItems: Item[];
  successImportedItemsCount: number;
  notImportedItemsCount: number;
  resultItems: ResultItem[];
};

export type ResultItem = {
  IsImportSuccessful: boolean;
  Message: string;
  CwbTemplateName: string;
  Title: string;
  Status: Status;
  CmsLink: string;
  CwbLink: string;
};

export type formDataRefMultiLocationImportType = {
  selectedItems: Item[];
  successImportedItemsCount: number;
  notImportedItemsCount: number;
  resultItems: ResultItem[];
  confirmItems: confirmItem[];
  groupedItems: groupedItem[];
  groupedItemsRestructured: StructureSubrowsData[];
  selectedGroupItems: groupedItem[];
};

export type groupedItem = {
  Id: string;
  TemplateName: string;
  MappingName: string;
  ScTemplate: string;
  DefaultLocation: string;
  DefaultLocationTitle: string;
  OpenerId: string;
};

export type confirmItem = {
  ItemId: string;
  ItemTitle: string;
  TemplateName: string;
  MappingId: string;
  MappingName: string;
  ScTemplate: string;
  DefaultLocation: string;
  DefaultLocationTitle: string;
};
