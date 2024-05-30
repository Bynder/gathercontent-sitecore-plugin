export type MappingData = {
  CwbProjectName: string;
  CwbProjectId: string;
  CwbTemplateName: string;
  SitecoreTemplates: SitecoreTemplate[];
  CwbProjects: CwbProject[];
  AddMappingModel: AddMappingModel;
  Rules: Rules;
  ScMappingId: string;
  SelectedFields: any[] | undefined;
  IsEdit: boolean;
  IsError: boolean;
  IsShowing: boolean;
  OptionsContentFolders: SitecoreField[] | undefined;
  OptionsTemplates: SitecoreField[] | undefined;
} & ErrorData;

export type ErrorData = {
  message?: string;
  status?: string;
};

export type SitecoreTemplate = {
  SitrecoreTemplateName: string;
  SitrecoreTemplateId: string;
  SitecoreFields: SitecoreField[];
};

export type SitecoreField = {
  SitrecoreFieldName: string;
  SitecoreFieldId: string;
  SitecoreFieldType?: string;
  Selected?: boolean;
};

export type CwbProject = {
  Id: string;
  Name: string;
};

export type AddMappingModel = {
  CwbTemplateId: string;
  CwbProjectId: string;
  SelectedTemplateId: string;
  CwbMappingTitle: string;
  OpenerId: string;
  DefaultLocation: string;
  DefaultLocationTitle: string;
};

export type Rules = {
  [key: string]: any;
  text: string;
  section: string;
  choice_radio: string;
  choice_checkbox: string;
  files: string;
};

export type SaveModel = {
  TemplateTabs: Tab[] | undefined;
  IsEdit: boolean | undefined;
  SelectedTemplateId: string | undefined;
  TemplateId: string | undefined;
  CwbMappingTitle: string | undefined;
  ScMappingId: string | undefined;
  DefaultLocation: string | undefined;
};

export type Tab = {
  TabName: string;
  Fields: TabsField[];
};

export type TabsField = {
  FieldName?: string;
  FieldId: string;
  SelectedScField: string;
  FieldType: string;
  OptionsContentFolderId: string;
  OptionsTemplateId: string;
};

export type formDataRefType = {
  DefaultLocation: string | undefined;
  DefaultLocationTitle: string | undefined;
  CwbProjectId: string | undefined;
  SelectedTemplateId: string | undefined;
  CwbTemplateId: string | undefined;
  SelectedFields: any[] | undefined;
  SelectedScOptionContentFolders: any[] | undefined;
  SelectedScOptionTemplates: any[] | undefined;

  // SitecoreFields: SitecoreField[];
};
