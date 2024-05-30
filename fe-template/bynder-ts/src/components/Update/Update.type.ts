export type UpdateData = {
  Filters: Filters;
  Items: Item[];
  Languages: Language[];
};

export type Language = {
  Name: string;
  IsoCode: string;
};

export type Item = {
  Id: string;
  ScTitle: string;
  ScTemplateName: string;
  Status: Status;
  CwbItem: Project;
  CwbProject: CwbProject;
  LastUpdatedInCwb: string;
  LastUpdatedInSitecore: string;
  CwbTemplate: Project;
  CmsLink: string;
  CwbLink: string;
};

export type CwbProject = {
  Id?: any;
  Name: string;
};

export type Filters = {
  Project?: any;
  Projects: Project[];
  Templates: Project[];
  Statuses: Status[];
};

export type Status = {
  Id: string;
  Name: string;
  Color?: any;
};

export type Project = {
  Id: string;
  Name: string;
};

export type formDataRefUpdateType = {
  selectedItems: Item[];
  notUpdatedItemsCount: number;
  successUpdatedItemsCount: number;
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
