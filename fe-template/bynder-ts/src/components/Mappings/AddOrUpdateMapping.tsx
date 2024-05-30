import { useState, useEffect, useRef } from "react";
import { deepEqual } from "fast-equals";

import { Accordion, AccordionTab } from "primereact/accordion";

import { canUseDOM, findItem, getUrlAPI, getUrlVars } from "../../utils/utils";
import usePrevious from "../../utils/hooks";

import "./AddOrUpdateMapping.scss";
import {
  CwbProject,
  MappingData,
  SaveModel,
  SitecoreField,
  SitecoreTemplate,
  Tab,
  TabsField,
  formDataRefType,
} from "./AddOrUpdateMapping.type";

import TreeView from "../ui/TreeView/TreeView";
import { TreeNodeModification } from "../ui/TreeView/TreeView.type";

declare global {
  interface Window {
    dialogClose: () => void;
  }
}

const AddOrUpdateMapping = () => {
  const [errorText, setErrorText] = useState<string>("");
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [data, setData] = useState<MappingData>();
  const prevData = usePrevious(data);
  const [activeTabIndex, setActiveTabIndex] = useState<number | null>(0);

  const cwbTemplateId = useRef(getUrlVars()["id"] || "");
  const scMappingId = useRef(getUrlVars()["scMappingId"] || "");

  useEffect(() => {
    (async () => {
      setIsLoading(true);

      try {
        const response = await fetch(
          `${getUrlAPI()}/api/sitecore/mappings/GetMapping?cwbTemplateId=${
            cwbTemplateId.current
          }&scMappingId=${scMappingId.current}`,
          {
            method: "GET",
          }
        );

        if (!response.ok) {
          setErrorText(`Network response was not ok.`);
          throw new Error("Network response was not ok");
        }

        const responseData: MappingData = await response.json();
        if (responseData.status !== "error") {
          setData({ ...responseData });
        } else {
          setErrorText(`Error: ${responseData.message}`);
        }
      } catch (error: any) {
        console.error(`${error?.message}`);
        setErrorText(`${error?.message}`);
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

  // ViewModel
  const [NotValid, setNotValid] = useState(false);
  const [NotValidCwbProject, setNotValidCwbProject] = useState(false);
  const [NotValidCwbTemplate, setNotValidCwbTemplate] = useState(false);
  const [NotValidScTemplate, setNotValidScTemplate] = useState(false);
  const [NotValidCwbMappingTitle, setNotValidCwbMappingTitle] = useState(false);

  //Fields

  const [CwbMappingTitle, setCwbMappingTitle] = useState(
    data?.AddMappingModel?.CwbMappingTitle || ""
  );

  const formDataRef = useRef<formDataRefType>({
    DefaultLocation: "",
    DefaultLocationTitle: "",
    CwbProjectId: "",
    SelectedTemplateId: "",
    CwbTemplateId: "",
    SelectedFields: [],
    SelectedScOptionContentFolders: [],
    SelectedScOptionTemplates: [],
    // SitecoreFields: [],
  });

  const [CwbTemplates, setCwbTemplates] = useState<CwbProject[]>([]);
  const [Tabs, setTabs] = useState<Tab[]>([]);
  const [CwbProjects, setCwbProjects] = useState<CwbProject[]>([]);
  const [SitecoreTemplates, setSitecoreTemplates] = useState<
    SitecoreTemplate[]
  >([]);
  const [SelectedCwbProject, setSelectedCwbProject] = useState<CwbProject>();
  const prevSelectedCwbProjectId = usePrevious(SelectedCwbProject?.Id);
  const [SelectedCwbTemplate, setSelectedCwbTemplate] = useState<CwbProject>();
  const prevSelectedCwbTemplateId = usePrevious(SelectedCwbTemplate?.Id);
  const [SelectedScTemplate, setSelectedScTemplate] =
    useState<SitecoreTemplate>();
  const prevSelectedScTemplateId = usePrevious(SelectedCwbTemplate?.Id);

  useEffect(() => {
    if (!deepEqual(data, prevData)) {
      formDataRef.current = {
        DefaultLocation: data?.AddMappingModel?.DefaultLocation,
        DefaultLocationTitle: data?.AddMappingModel?.DefaultLocationTitle,
        CwbProjectId: data?.AddMappingModel?.CwbProjectId,
        SelectedTemplateId: data?.IsEdit
          ? data?.AddMappingModel?.SelectedTemplateId
          : data?.SitecoreTemplates?.[0]?.SitrecoreTemplateId,
        CwbTemplateId: data?.AddMappingModel?.CwbTemplateId,
        SelectedFields: data?.SelectedFields || [],
        SelectedScOptionContentFolders: data?.OptionsContentFolders || [],
        SelectedScOptionTemplates: data?.OptionsTemplates || [],
        // SitecoreFields: [],
      };

      setCwbMappingTitle(data?.AddMappingModel?.CwbMappingTitle || "");
      setSitecoreTemplates(data?.SitecoreTemplates || []);
      setCwbProjects(data?.CwbProjects || []);
      setSelectedCwbProject(
        data?.IsEdit
          ? findItem("Id", data?.CwbProjects, data?.CwbProjectId)
          : data?.CwbProjects?.[0]
      );

      setSelectedScTemplate(
        findItem(
          "SitrecoreTemplateId",
          data?.SitecoreTemplates,
          data?.AddMappingModel?.SelectedTemplateId || "0"
        )
      );

      cwbProjectChanged();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data]);

  const validate = () => {
    let isValid = true;

    if (CwbMappingTitle === "" || NotValidCwbMappingTitle) {
      setNotValid(true);
      isValid = false;
    } else {
      setNotValid(false);
    }

    if (SelectedCwbProject?.Id === "0") {
      setNotValidCwbProject(true);
      isValid = false;
    } else {
      setNotValidCwbProject(false);
    }

    if (formDataRef?.current?.SelectedTemplateId === "0") {
      setNotValidScTemplate(true);
      isValid = false;
    } else {
      setNotValidScTemplate(false);
    }

    if (SelectedCwbTemplate?.Id === "0") {
      setNotValidCwbTemplate(true);
      isValid = false;
    } else {
      setNotValidCwbTemplate(false);
    }

    return isValid;
  };

  const scTemplateChanged = () => {
    // formDataRef.current.SitecoreFields = SelectedScTemplate?.SitecoreFields;
    formDataRef.current.SelectedTemplateId =
      SelectedScTemplate?.SitrecoreTemplateId;
    // setTabs([]);
    cwbTemplateChanged();
  };

  const cwbProjectChanged = async () => {
    if (SelectedCwbProject !== undefined && SelectedCwbProject !== null) {
      try {
        const response = await fetch(
          `${getUrlAPI()}/api/sitecore/mappings/GetTemplatesByProjectId?cwbProjectId=${
            SelectedCwbProject?.Id
          }`,
          {
            method: "GET",
          }
        );

        if (!response.ok) {
          setErrorText(`Network response was not ok.`);
          throw new Error("Network response was not ok");
        }

        const responseData = await response.json();
        if (responseData.status !== "error") {
          setCwbTemplates([...responseData]);
          if (data?.IsEdit) {
            setSelectedCwbTemplate(
              findItem("Id", responseData, formDataRef?.current?.CwbTemplateId)
            );
          } else {
            setSelectedCwbTemplate(responseData[0]);
          }
        } else {
          setErrorText(`Error: ${responseData?.message}`);
        }
      } catch (error) {
        setErrorText(`Error: ${error}`);
      }
    }
  };

  const cwbTemplateChanged = async () => {
    if (SelectedCwbTemplate !== undefined && SelectedCwbTemplate !== null) {
      setTabs([]);
      try {
        const response = await fetch(
          `${getUrlAPI()}/api/sitecore/mappings/GetFieldsByTemplateId?cwbTemplateId=${
            SelectedCwbTemplate.Id
          }`,
          {
            method: "GET",
          }
        );
        if (!response.ok) {
          setErrorText(`Network response was not ok.`);
          throw new Error("Network response was not ok");
        }
        const data = await response.json();

        if (data.status !== "error") {
          for (let t = 0; t < data.length; t++) {
            for (let i = 0; i < data[t].Fields.length; i++) {
              let currentElement = data[t].Fields[i];
              let selectedItem = findItem(
                "CwbFieldId",
                formDataRef?.current?.SelectedFields,
                currentElement.FieldId
              );

              if (
                currentElement.SelectedScField === null ||
                currentElement.SelectedScField === undefined
              ) {
                currentElement.SelectedScField = "0";
              }

              if (
                currentElement.OptionsContentFolderId === null ||
                currentElement.OptionsContentFolderId === undefined
              ) {
                currentElement.OptionsContentFolderId = "0";
              }

              if (
                currentElement.OptionsTemplateId === null ||
                currentElement.OptionsTemplateId === undefined
              ) {
                currentElement.OptionsTemplateId = "0";
              }

              if (selectedItem !== null && selectedItem !== undefined) {
                currentElement.SelectedScField =
                  selectedItem?.SitecoreTemplateId || "0";
                currentElement.OptionsContentFolderId =
                  selectedItem?.OptionsContentFolderId || "0";
                currentElement.OptionsTemplateId =
                  selectedItem?.OptionsTemplateId || "0";
              }
            }
          }
          setActiveTabIndex(0);
          setTabs([...data]);
        } else {
          setErrorText(`Error: ${data?.message}`);
        }
      } catch (error) {
        setErrorText(`Error: ${error}`);
      }
    }
  };

  const CwbMappingTitleChanged = async (title: string) => {
    if (title && !!title.trim().length && title !== CwbMappingTitle) {
      const trimTitle = title.trim();
      setCwbMappingTitle(trimTitle);
      try {
        const response = await fetch(
          `${getUrlAPI()}/api/sitecore/mappings/CheckIfMappingExists?mappingName=${trimTitle}`,
          {
            method: "POST",
          }
        );

        if (!response.ok) {
          setErrorText(`Network response was not ok.`);
          throw new Error("Network response was not ok");
        }

        const isMappingExists = await response.json();

        if (isMappingExists) {
          setNotValidCwbMappingTitle(true);
          setNotValid(true);
          setErrorText(
            `Error: A mapping with the name "${trimTitle}" already exists! Choose a unique name.`
          );
        } else {
          setNotValidCwbMappingTitle(false);
          setNotValid(false);
          setCwbMappingTitle(trimTitle);
          setErrorText("");
        }
      } catch (error: any) {
        setErrorText(`Error: ${error.message}`);
      }
    }
  };

  //tree

  const [isLocationTreeShow, setIsLocationTreeShow] = useState<boolean>(false);
  const mappingTreeContainer = useRef<HTMLDivElement>(null);

  const openDropTree = (e: React.MouseEvent<HTMLInputElement, MouseEvent>) => {
    e.preventDefault();
    e.stopPropagation();
    tabCloseDropTree(false);

    if (!mappingTreeContainer?.current?.contains(e.target as HTMLElement)) {
      setIsLocationTreeShow(!isLocationTreeShow);
    }
  };

  const closeDropTree = (
    e: React.MouseEvent<HTMLElement, MouseEvent> | false = false
  ) => {
    if (
      e &&
      mappingTreeContainer?.current?.contains(e.target as HTMLElement) &&
      isLocationTreeShow
    ) {
      return false;
    }
    setIsLocationTreeShow(false);
  };

  const handleLocationSelect = (node: TreeNodeModification) => {
    formDataRef.current.DefaultLocation = node.key as string;
    formDataRef.current.DefaultLocationTitle = node.data.path as string;
    setIsLocationTreeShow(false);
  };

  const tabTreeContainer = useRef<HTMLElement | null>(null);
  const tabTreeInput = useRef<HTMLInputElement | null>(null);

  const tabOpenDropTree = (
    e: React.MouseEvent<HTMLInputElement, MouseEvent>
  ) => {
    e.preventDefault();
    e.stopPropagation();

    closeDropTree(false);

    if (tabTreeInput.current) {
      if (tabTreeInput.current !== e.target) {
        setTimeout(() => {
          tabOpenDropTree(e);
        }, 100);
      }
      tabCloseDropTree();
      return false;
    }

    const clickTarget = e.target as HTMLInputElement;
    tabTreeInput.current = clickTarget;
    const targetId = clickTarget.dataset.openerid || "";
    const targetTree = document.getElementById(targetId);
    if (targetTree) {
      tabTreeContainer.current = targetTree;
      targetTree.classList.remove("d-none");
      targetTree.classList.add("d-block");
    }
  };

  const tabCloseDropTree = (
    e: React.MouseEvent<HTMLElement, MouseEvent> | false = false
  ) => {
    if (e && tabTreeContainer?.current?.contains(e.target as HTMLElement)) {
      return false;
    }
    if (tabTreeContainer.current) {
      tabTreeContainer.current.classList.add("d-none");
      tabTreeContainer.current.classList.remove("d-block");
      tabTreeInput.current = null;
      tabTreeContainer.current = null;
    }
  };

  const tabHandleLocationSelect = (
    node: TreeNodeModification,
    field: TabsField,
    props: "OptionsContentFolderId" | "OptionsTemplateId"
  ) => {
    if (tabTreeInput.current) {
      tabTreeInput.current.value = node.label as string;
      field[props] = node.key as string;
      tabCloseDropTree();
    }
  };

  const saveMapping = async () => {
    if (validate()) {
      let model: SaveModel = {
        TemplateTabs: Tabs,
        IsEdit: data?.IsEdit,
        SelectedTemplateId: formDataRef?.current?.SelectedTemplateId,
        TemplateId: SelectedCwbTemplate?.Id,
        CwbMappingTitle: CwbMappingTitle,
        ScMappingId: data?.ScMappingId,
        DefaultLocation: formDataRef?.current?.DefaultLocation,
      };

      try {
        const response = await fetch(
          `${getUrlAPI()}/api/sitecore/mappings/Post`,
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify(model),
          }
        );

        if (!response.ok) {
          setErrorText(`Network response was not ok.`);
          throw new Error("Network response was not ok");
        } else {
          if (canUseDOM() && window?.opener && window?.top) {
            window.opener.location.reload(true);
            window.top.dialogClose();
          }
        }
      } catch (error) {
        setErrorText(`Error: ${error}`);
      }
    }
  };

  const GetCurrentFields = (item: TabsField) => {
    const fieldType = item.FieldType;
    const allowedFields = data?.Rules[fieldType];
    if (typeof allowedFields === "undefined") {
      return [SelectedScTemplate?.SitecoreFields[0]];
    } else {
      if (allowedFields !== null) {
        const allowedFieldsArr = allowedFields.split(",");
        const currentCollection = SelectedScTemplate?.SitecoreFields || [];
        const resultCollection: SitecoreField[] = [];
        resultCollection.push(
          SelectedScTemplate?.SitecoreFields[0] as SitecoreField
        );

        for (let i = 0; i < currentCollection.length; i++) {
          const currentElement = currentCollection[i];
          const selectedItem = findItem(
            "CwbFieldId",
            formDataRef?.current?.SelectedFields,
            item.FieldId
          );

          if (selectedItem !== null && selectedItem !== undefined) {
            if (
              currentElement.SitecoreFieldId ===
              selectedItem?.SitecoreTemplateId
            ) {
              currentElement.Selected = true;
            }
          }

          for (let f = 0; f < allowedFieldsArr.length; f++) {
            const field = allowedFieldsArr[f];
            if (currentElement.SitecoreFieldType === field.trim()) {
              resultCollection.push(currentElement);
            }
          }
        }
        return resultCollection;
      }
    }
    return [SelectedScTemplate?.SitecoreFields[0]];
  };

  const GetOptionContentFolders = () => {
    return formDataRef.current.SelectedScOptionContentFolders;
  };

  const GetOptionTemplates = () => {
    return formDataRef.current.SelectedScOptionTemplates;
  };

  const returnFieldName = (item: TabsField) => {
    if (item.FieldName === null) {
      return "[Guideline]";
    } else {
      return item.FieldName;
    }
  };

  const RenderOptionSettings = (item: TabsField) => {
    const fieldType = item.FieldType;
    if (fieldType === "choice_checkbox" || fieldType === "choice_radio") {
      return true;
    } else {
      return false;
    }
  };

  useEffect(() => {
    if (prevSelectedCwbProjectId !== SelectedCwbProject?.Id) {
      cwbProjectChanged();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [SelectedCwbProject?.Id]);

  useEffect(() => {
    if (prevSelectedCwbTemplateId !== SelectedCwbTemplate?.Id) {
      // setTabs([]);
      cwbTemplateChanged();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [SelectedCwbTemplate?.Id]);

  useEffect(() => {
    if (prevSelectedScTemplateId !== SelectedScTemplate?.SitrecoreTemplateId) {
      scTemplateChanged();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [SelectedScTemplate?.SitrecoreTemplateId]);

  return (
    <>
      <div
        className="content-workflow-dialog"
        onClick={(e) => {
          closeDropTree(e);
          tabCloseDropTree(e);
        }}
      >
        {errorText && <h1>{errorText}</h1>}

        <div className="content-workflow-dialog__note">
          <p>
            Choose the fields you wish to map from Content Workflow into
            Sitecore. Only map the fields you need to import.
          </p>
        </div>

        <div className="mapping-fields">
          <div className="mapping-fields__item">
            <input
              type="text"
              defaultValue={CwbMappingTitle || ""}
              className={`mapping-input block content-workflow-input ${
                NotValid ? "notvalid" : ""
              }`}
              placeholder="Mapping name *"
              name="CwbMappingTitle"
              onBlur={(e) => {
                CwbMappingTitleChanged(e.target.value);
              }}
              autoComplete="off"
            />
          </div>

          <div className="mapping-fields__item">
            <div className="tree-view">
              <input
                className="mapping-input block content-workflow-input"
                placeholder="Default location"
                name="DefaultLocationTitle"
                value={formDataRef?.current?.DefaultLocationTitle || ""}
                type="text"
                readOnly={true}
                autoComplete="off"
                onClick={openDropTree}
              />
              <div className="tree-view-container" ref={mappingTreeContainer}>
                <TreeView
                  onLocationSelect={handleLocationSelect}
                  className={isLocationTreeShow ? "d-block" : "d-none"}
                />
              </div>
            </div>

            <input
              className="d-none"
              defaultValue={formDataRef?.current?.DefaultLocation || ""}
              type="text"
              data-openerid={data?.AddMappingModel?.OpenerId || ""}
              name="DefaultLocation"
            />
          </div>
        </div>

        <div className="mapping-tabs-header">
          <div className="mapping-heading">
            <strong className="heading">Content Workflow:</strong>

            <select
              className={`custom_selected sitecore-template-select content-workflow-input ${
                NotValidCwbProject ? "notvalid" : ""
              } ${data?.IsEdit ? "disabled" : ""}`}
              disabled={data?.IsEdit}
              name="CwbProjects"
              value={SelectedCwbProject?.Id}
              onChange={(e) => {
                setSelectedCwbProject(
                  findItem("Id", data?.CwbProjects, e.target.value)
                );
              }}
            >
              {CwbProjects.map((item: CwbProject) => {
                return (
                  <option key={item.Id} value={item.Id}>
                    {item.Name}
                  </option>
                );
              })}
            </select>

            <select
              className={`custom_selected sitecore-template-select content-workflow-input ${
                NotValidCwbTemplate ? "notvalid" : ""
              } ${data?.IsEdit ? "disabled" : ""}`}
              disabled={data?.IsEdit}
              name="CwbTemplates"
              value={SelectedCwbTemplate?.Id}
              onChange={(e) => {
                setSelectedCwbTemplate(
                  findItem("Id", CwbTemplates, e.target.value)
                );
              }}
            >
              {CwbTemplates.map((item) => {
                return (
                  <option key={item.Id} value={item.Id}>
                    {item.Name}
                  </option>
                );
              })}
            </select>
          </div>
          <div className="mapping-heading">
            <strong className="heading">Sitecore:</strong>

            <select
              className={`custom_selected sitecore-template-select content-workflow-input ${
                NotValidScTemplate ? "notvalid" : ""
              } ${data?.IsEdit ? "disabled" : ""}`}
              disabled={data?.IsEdit}
              name="SitecoreTemplates"
              value={SelectedScTemplate?.SitrecoreTemplateId}
              onChange={(e) => {
                setSelectedScTemplate(
                  findItem(
                    "SitrecoreTemplateId",
                    SitecoreTemplates,
                    e.target.value
                  )
                );
              }}
            >
              {SitecoreTemplates.map((item: SitecoreTemplate) => {
                return (
                  <option
                    key={item.SitrecoreTemplateId}
                    value={item.SitrecoreTemplateId}
                  >
                    {item.SitrecoreTemplateName}
                  </option>
                );
              })}
            </select>
          </div>
        </div>

        {!!Tabs?.length && (
          <div className="mapping-tabs-wrapper custom-scrollbar">
            <Accordion
              activeIndex={activeTabIndex}
              onTabChange={(e) => setActiveTabIndex(e.index as number)}
            >
              {Tabs.map((item, index) => {
                return (
                  <AccordionTab
                    header={item.TabName}
                    key={item.TabName + index}
                  >
                    {item.Fields.map((field, index) => {
                      const currentFieldsArr = GetCurrentFields(field);

                      const optionContentFoldersArr =
                        GetOptionContentFolders() || [];
                      const indexConentFolder =
                        optionContentFoldersArr.findIndex(
                          (obj) =>
                            obj.SitecoreFieldId === field.OptionsContentFolderId
                        );
                      const titleConentFolder =
                        indexConentFolder !== -1
                          ? optionContentFoldersArr[indexConentFolder]
                              .SitrecoreFieldName
                          : null;

                      const optionTemplatesArr = GetOptionTemplates() || [];
                      const indexTemplate = optionTemplatesArr.findIndex(
                        (obj) => obj.SitecoreFieldId === field.OptionsTemplateId
                      );
                      const titleTemplate =
                        indexTemplate !== -1
                          ? optionTemplatesArr[indexTemplate].SitrecoreFieldName
                          : null;

                      return (
                        <div
                          className="mapping-tab-row"
                          key={(field.FieldName as string) + index}
                        >
                          <input
                            className="content-workflow-input"
                            type="text"
                            value={returnFieldName(field)}
                            disabled
                          />

                          <div className="mapping-tab-row__arrow"></div>

                          <select
                            className="custom-selected content-workflow-input"
                            defaultValue={field.SelectedScField}
                            onChange={(e) => {
                              field.SelectedScField = e.target.value;
                            }}
                          >
                            {currentFieldsArr.map((option, index) => {
                              return (
                                <option
                                  key={
                                    (option?.SitecoreFieldId as string) + index
                                  }
                                  value={option?.SitecoreFieldId}
                                >
                                  {option?.SitrecoreFieldName}
                                </option>
                              );
                            })}
                          </select>

                          {RenderOptionSettings(field) && (
                            <>
                              <div className="mapping-tab-row__arrow"></div>
                              {/* <select
                                className="custom-selected content-workflow-input"
                                defaultValue={field.OptionsContentFolderId}
                                onChange={(e) => {
                                  field.OptionsContentFolderId = e.target.value;
                                }}
                              >
                                {optionContentFoldersArr.map(
                                  (option, index) => {
                                    return (
                                      <option
                                        key={
                                          (option?.SitecoreFieldId as string) +
                                          index
                                        }
                                        value={option?.SitecoreFieldId}
                                      >
                                        {option?.SitrecoreFieldName}
                                      </option>
                                    );
                                  }
                                )}
                              </select> */}
                              <div className="tree-view">
                                <input
                                  className="mapping-input block content-workflow-input"
                                  placeholder="Default location"
                                  name="DefaultLocationTitle"
                                  defaultValue={titleConentFolder}
                                  type="text"
                                  readOnly={true}
                                  autoComplete="off"
                                  onClick={tabOpenDropTree}
                                  data-openerid={
                                    field.FieldId
                                      ? field.FieldId +
                                        "-OptionsContentFolderId"
                                      : ""
                                  }
                                />
                                <div className="tree-view-container">
                                  <TreeView
                                    onLocationSelect={(node) => {
                                      tabHandleLocationSelect(
                                        node,
                                        field,
                                        "OptionsContentFolderId"
                                      );
                                    }}
                                    className={"d-none"}
                                    id={
                                      field.FieldId
                                        ? field.FieldId +
                                          "-OptionsContentFolderId"
                                        : ""
                                    }
                                    getTopLvlUrl="/api/sitecore/DropTree/GetOptionsContentFoldersTopLevelNode"
                                  />
                                </div>
                              </div>

                              <div className="mapping-tab-row__arrow"></div>
                              {/* <select
                                className="custom-selected content-workflow-input"
                                defaultValue={field.OptionsTemplateId}
                                onChange={(e) => {
                                  field.OptionsTemplateId = e.target.value;
                                }}
                              >
                                {optionTemplatesArr.map((option, index) => {
                                  return (
                                    <option
                                      key={
                                        (option?.SitecoreFieldId as string) +
                                        index
                                      }
                                      value={option?.SitecoreFieldId}
                                    >
                                      {option?.SitrecoreFieldName}
                                    </option>
                                  );
                                })}
                              </select> */}
                              <div className="tree-view">
                                <input
                                  className="mapping-input block content-workflow-input"
                                  placeholder="Default location"
                                  name="DefaultLocationTitle"
                                  defaultValue={titleTemplate}
                                  type="text"
                                  readOnly={true}
                                  autoComplete="off"
                                  onClick={tabOpenDropTree}
                                  data-openerid={
                                    field.FieldId
                                      ? field.FieldId + "-OptionsTemplateId"
                                      : ""
                                  }
                                />
                                <div className="tree-view-container">
                                  <TreeView
                                    onLocationSelect={(node) => {
                                      tabHandleLocationSelect(
                                        node,
                                        field,
                                        "OptionsTemplateId"
                                      );
                                    }}
                                    className={"d-none"}
                                    id={
                                      field.FieldId
                                        ? field.FieldId + "-OptionsTemplateId"
                                        : ""
                                    }
                                    getTopLvlUrl="/api/sitecore/DropTree/GetOptionsTemplatesTopLevelNode"
                                    getChildrenLvlUrl="/api/sitecore/DropTree/GetOnlyTemplatesChildren?id="
                                  />
                                </div>
                              </div>
                            </>
                          )}
                        </div>
                      );
                    })}
                  </AccordionTab>
                );
              })}
            </Accordion>
          </div>
        )}

        <footer className="content-workflow-dialog__footer">
          <div className="footer-group">
            <input
              className="btn"
              id="AddMore"
              type="button"
              value="Save mapping configuration"
              onClick={saveMapping}
            />
            <p>You can always change your mapping later.</p>
          </div>

          <div className="help-link">
            <a href="mailto:support@gathercontent.com">Need help?</a>
          </div>
        </footer>
      </div>
    </>
  );
};

export default AddOrUpdateMapping;
