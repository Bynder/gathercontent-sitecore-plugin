namespace Bynder.Content.SitecoreConnector.Managers.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Web;
    using Bynder.Content.SitecoreConnector.GatherContentService.Interfaces;
    using Interfaces;
    using Models.ImportItems.New;
    using Models.Mapping;
    using Models.UpdateItems;
    using Models.UpdateItems.New;
    using Constants = SitecoreRepositories.Repositories.Constants;
    using Core.DependencyInjection;
    using Core.Entities;
    using Core.Interfaces;
    using Core.Models.Import;
    using Core.Models.Mapping;
    using Sitecore.Data;
    using Sitecore.Configuration;
    using Sitecore.Data.Items;
    using Sitecore.Globalization;
    using Sitecore.SecurityModel;

    [Service(typeof(IUpdateManager))]
    public class UpdateManager : BaseManager, IUpdateManager
	{
		protected IItemsRepository ItemsRepository;
        protected IMappingRepository MappingRepository;
        protected IItemsService ItemsService;
		protected ILogger Log;
        protected readonly Database ContextDatabase;

        public UpdateManager(
			IItemsRepository itemsRepository,
			IMappingRepository mappingRepository,
			IItemsService itemsService,
			IAccountsService accountsService,
			IProjectsService projectsService,
			ITemplatesService templateService,
			ICacheManager cacheManager,
			ILogger logger,
            IAccountsRepository accountsRepository)
			: base(accountsService, projectsService, templateService, cacheManager, accountsRepository)
		{
			ItemsRepository = itemsRepository;
			MappingRepository = mappingRepository;
			ItemsService = itemsService;
			Log = logger ?? new NullLogger();
            ContextDatabase = Factory.GetDatabase("master");
        }

		#region Utilities

		private UpdateModel MapUpdateItems(IEnumerable<CmsItem> cmsItems)
		{
			var model = new UpdateModel();
			var projectsDictionary = new Dictionary<int, Project>();
			var templatesDictionary = new Dictionary<int, CWBTemplate>();

			var statuses = new List<CwbStatusModel>();
			var templates = new List<CwbTemplateModel>();
			var projects = new List<CwbProjectModel>();

			var items = new List<UpdateItemModel>();

			foreach (var cmsItem in cmsItems)
			{
				var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == "CWB Content Id");
				if (idField != null && !string.IsNullOrEmpty(idField.Value.ToString()))
				{
					ItemEntity entity = null;
					try
					{
						entity = ItemsService.GetSingleItem(idField.Value.ToString());
					}
					catch (WebException exception)
					{
						Log.Error("ContentWorkflow message. Api Server error has happened during getting Item with id = " + idField.Value, exception);
						using (var response = exception.Response)
						{
							var httpResponse = (HttpWebResponse)response;
							if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
							{
								throw;
							}
						}
					}
					if (entity != null)
					{
						var cwbItem = entity.Data;
						var project = GetProject(projectsDictionary, cwbItem.ProjectId);
						if (project != null)
						{
							if (projects.All(i => i.Id != project.Id.ToString()))
							{
								projects.Add(new CwbProjectModel
								{
									Id = project.Id.ToString(),
									Name = project.Name
								});
							}
						}
						if (cwbItem.TemplateId.HasValue)
						{
							var template = GetTemplate(templatesDictionary, cwbItem.TemplateId.Value);

							if (templates.All(i => i.Id != template.Id.ToString()))
							{
								templates.Add(new CwbTemplateModel
								{
									Id = template.Id.ToString(),
									Name = template.Name,
								});
							}

							string cwbLink = null;
							if (!string.IsNullOrEmpty(CwbAccountSettings.GatherContentUrl))
							{
								cwbLink = CwbAccountSettings.GatherContentUrl + "/item/" + cwbItem.Id;
							}
							var dateFormat = CwbAccountSettings.DateFormat;
							if (string.IsNullOrEmpty(dateFormat))
							{
								dateFormat = Constants.DateFormat;
							}
							var cmsLink =
								string.Format(
									"http://{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master&sc_bw=1",
									HttpContext.Current.Request.Url.Host, cmsItem.Id);


							var lastUpdate = new DateTime();
							string cmsTemplateName = null;
							var lastUpdateField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == "Last Sync Date");
							if (lastUpdateField != null)
							{
								lastUpdate = (DateTime)lastUpdateField.Value;
							}

							var cmsTemplateNameField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == "Template");
							if (cmsTemplateNameField != null)
							{
								cmsTemplateName = cmsTemplateNameField.Value.ToString();
							}

							var status = cwbItem.Status.Data;

							if (statuses.All(i => i.Id != status.Id))
							{
								statuses.Add(new CwbStatusModel
								{
									Id = status.Id,
									Name = status.Name,
									Color = status.Color,
									ProjectId = cwbItem.ProjectId.ToString()
								});
							}

							var listItem = new UpdateItemModel
							{
								CmsId = cmsItem.Id,
								Title = cmsItem.Title,
								CmsLink = cmsLink,
								CwbLink = cwbLink,
								LastUpdatedInCms = lastUpdate.ToString(dateFormat),
								Project = new CwbProjectModel { Name = project.Name },
								CmsTemplate = new CmsTemplateModel { Name = cmsTemplateName },
								CwbTemplate = new CwbTemplateModel
								{
									Id = template.Id.ToString(),
									Name = template.Name
								},
								Status = new CwbStatusModel
								{
									Id = status.Id,
									Name = status.Name,
									Color = status.Color
								},
								CwbItem = new CwbItemModel
								{
									Id = cwbItem.Id.ToString(),
									Title = cwbItem.Name,
									LastUpdatedInCwb = TimeZoneInfo.ConvertTimeFromUtc(cwbItem.Updated.Date, TimeZoneInfo.Local).ToString(dateFormat),
								}
							};

							items.Add(listItem);

						}
					}
				}
			}

			items = items.OrderBy(item => item.Status.Name).ToList();

			model.Items = items;
			model.Filters = new UpdateFiltersModel
			{
				Projects = projects,
				Statuses = statuses,
				Templates = templates
			};

			return model;
		}


		private CWBTemplate GetTemplate(Dictionary<int, CWBTemplate> templates, int templateId)
		{
			CWBTemplate template;
			templates.TryGetValue(templateId, out template);

			if (template == null)
			{
				template = GetCwbTemplateEntity(templateId.ToString()).Data;
				templates.Add(templateId, template);
			}

			return template;
		}

		private Project GetProject(Dictionary<int, Project> projects, int projectId)
		{
			Project project;
			projects.TryGetValue(projectId, out project);

			if (project == null)
			{
				project = GetCwbProjectEntity(projectId.ToString()).Data;
				projects.Add(projectId, project);
			}

			return project;
		}

		private object GetValue(IEnumerable<Element> fields)
		{
			string value = string.Join("", fields.Select(i => i.Value));
			return value;
		}

	    private Dictionary<string, string> GetOptions(IEnumerable<Element> fields)
	    {
	        var result = new Dictionary<string, string>();
	        foreach (Element field in fields)
	        {
	            if (field.Options != null)
	            {
	                foreach (var option in field.Options.Where(x => x.Selected))
	                {
	                    result.Add(option.Name, option.Label);
	                }
	            }
	        }
	        return result;
	    }


        #endregion


        public UpdateModel GetItemsForUpdate(string itemId, string languageId)
		{
			var cmsItems = ItemsRepository.GetItems(itemId, languageId).ToList();
			var model = MapUpdateItems(cmsItems);
			return model;
		}

        private List<Sitecore.Data.Items.Item> CreateChildOptions(Sitecore.Data.Items.Item parentItem, List<string> itemNames, ID templateID)
        {
            if (parentItem != null && itemNames.Any() && !templateID.IsNull)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(parentItem.Language))
                    {
                        using (new EnforceVersionPresenceDisabler())
                        {
                            foreach (var itemName in itemNames)
                            {
                                Sitecore.Data.Items.Item createdOption = null;
                                try
                                {
                                    var templateOption = ContextDatabase.GetTemplate(templateID);
                                    var validName = ItemUtil.ProposeValidItemName(itemName);
                                    createdOption = parentItem.Add(validName, templateOption);
                                }
                                catch (Exception ex)
                                {
                                    Sitecore.Diagnostics.Log.Error("Cannot create options item.", ex, this);
                                    throw new Exception(string.Format("Your template({0}) is not inherited from the CWB Linked Item.", createdOption.TemplateName));
                                }
                            }
                        }
                    }
                }
            }

            return parentItem.Axes.GetDescendants().ToList();
        }

        public List<ItemResultModel> UpdateItems(string itemId, List<UpdateListIds> models, string language)
		{
			var model = new List<ItemResultModel>();
			var cwbItems = new Dictionary<CWBItem, string>();

            ItemEntity itemEntity = null;
            var listOptions = new List<string>();
            var finalListOptions = new List<Item>();

            if (models != null && models.Any())
            {
                itemEntity = ItemsService.GetSingleItem(models.FirstOrDefault().CWBId);
            }

            foreach (var item in models)
			{
				var cwbItem = ItemsService.GetSingleItem(item.CWBId).Data;
                cwbItems.Add(cwbItem, item.CMSId);
			}

            var cmsIdForMappingTemplate = cwbItems.FirstOrDefault().Value;
            var templateMappingSc = MappingRepository.GetMappingByItemId(cmsIdForMappingTemplate, language);

            foreach (var configs in itemEntity.Data.Config)
            {
                foreach (var element in configs.Elements)
                {
                    if (element.Type.Equals("choice_checkbox") || element.Type.Equals("choice_radio"))
                    {
                        var currentFieldMapping = templateMappingSc.FieldMappings.Where(m => m.CwbField.Id.Equals(element.Name)).FirstOrDefault();

                        if (currentFieldMapping != null)
                        {
                            var currentOptionsTemplateId = currentFieldMapping.CmsField.TemplateField.OptionsTemplateId;
                            var currentOptionsContentFolderId = currentFieldMapping.CmsField.TemplateField.OptionsContentFolderId;

                            if (!string.IsNullOrEmpty(currentOptionsTemplateId) && !string.IsNullOrEmpty(currentOptionsContentFolderId))
                            {
                                var optionsTemplateItem = ContextDatabase.GetItem(new ID(currentOptionsTemplateId));
                                var optionsContentFolderItem = ContextDatabase.GetItem(new ID(currentOptionsContentFolderId));

                                var existingOptionList = optionsContentFolderItem.Axes.GetDescendants()
                                    .Where(t => t.TemplateName == optionsTemplateItem.Name)
                                    .Select(x => x.Name);

                                var cwbOptionList = new List<string>();

                                foreach (var option in element.Options)
                                {
                                    if (!string.IsNullOrEmpty(option.Label))
                                    {
                                        cwbOptionList.Add(option.Label);
                                    }
                                }

                                var needsToAdd = cwbOptionList.Where(x => !existingOptionList.Contains(x)).ToList();

                                var createdOptions = CreateChildOptions(optionsContentFolderItem, needsToAdd, optionsTemplateItem.ID);
                                if (createdOptions != null && createdOptions.Any())
                                {
                                    finalListOptions.AddRange(createdOptions);
                                }
                            }
                        }
                    }
                }
            }

            //var templates = MappingRepository.GetMappings();
            var templatesDictionary = new Dictionary<int, CWBTemplate>();

			foreach (var item in cwbItems)
			{
				CWBItem cwbItem = item.Key; //cwb item
				string cmsId = item.Value; // corresponding cms id
				var itemResponseModel = new ItemResultModel
				{
					IsImportSuccessful = true,
					ImportMessage = "Update Successful"
				};
				if (!string.IsNullOrEmpty(CwbAccountSettings.GatherContentUrl))
				{
					itemResponseModel.CwbLink = string.Concat(CwbAccountSettings.GatherContentUrl, "/item/", cwbItem.Id);
				}
				itemResponseModel.CwbItem = new CwbItemModel
				{
					Id = cwbItem.Id.ToString(),
					Title = cwbItem.Name
				};

				itemResponseModel.Status = new CwbStatusModel
				{
					Color = cwbItem.Status.Data.Color,
					Name = cwbItem.Status.Data.Name,
				};

				CWBTemplate cwbTemplate;
				int templateId = cwbItem.TemplateId.Value;
				templatesDictionary.TryGetValue(templateId, out cwbTemplate);
				if (cwbTemplate == null)
				{
					cwbTemplate = TemplatesService.GetSingleTemplate(templateId.ToString()).Data;
					templatesDictionary.Add(templateId, cwbTemplate);
				}

				itemResponseModel.CwbTemplate = new CwbTemplateModel
				{
					Id = cwbTemplate.Id.ToString(),
					Name = cwbTemplate.Name
				};
				string cmsLink = ItemsRepository.GetCmsItemLink(HttpContext.Current.Request.Url.Scheme, HttpContext.Current.Request.Url.Host, cmsId);
				itemResponseModel.CmsLink = cmsLink;
				itemResponseModel.CmsId = cmsId;

				//MappingResultModel cmsItem;
				//TryMapItem(cwbItem, cwbTemplate, templates, out cmsItem);
				//result.Add(cmsItem);
				List<Element> cwbFields = cwbItem.Config.SelectMany(i => i.Elements).ToList();

				//var templateMapping = templates.First(x => x.CwbTemplate.CwbTemplateId == cwbItem.TemplateId.ToString());

				var templateMapping = MappingRepository.GetMappingByItemId(cmsId, language);
				if (templateMapping != null) // template found, now map fields here
				{
					var cwbContentIdField = templateMapping.FieldMappings.FirstOrDefault(fieldMapping => fieldMapping.CmsField.TemplateField.FieldName == "CWB Content Id");
					if (cwbContentIdField != null)
					{
						templateMapping.FieldMappings.Remove(cwbContentIdField);
					}

					var files = new List<File>();
					if (cwbItem.Config.SelectMany(config => config.Elements).Any(element => element.Type == "files"))
					{
						foreach (var file in ItemsService.GetItemFiles(cwbItem.Id.ToString()).Data)
						{
							files.Add(new File
							{
								FileName = file.FileName,
								Url = file.Url,
								FieldId = file.Field,
								UpdatedDate = file.Updated,
                                FileId = file.Id
                            });
						}
					}

					bool fieldError = CheckFieldError(templateMapping, cwbFields, files, itemResponseModel);

					if (!fieldError)
					{
						var cmsContentIdField = new FieldMapping
						{
							CmsField = new CmsField
							{
								TemplateField = new CmsTemplateField { FieldName = "CWB Content Id" },
								Value = cwbItem.Id.ToString()
							}
						};
						templateMapping.FieldMappings.Add(cmsContentIdField);

						var cmsItem = new CmsItem
						{
							Template = templateMapping.CmsTemplate,
							Title = cwbItem.Name,
							Fields = templateMapping.FieldMappings.Select(x => x.CmsField).ToList(),
							Language = language,
							Id = cmsId
						};

						var fieldMappings = templateMapping.FieldMappings;

						// one CMS text field can be mapped to several CWB fields
						// in this case we concatenate their texts and put into one CMS field
						foreach (IGrouping<string, FieldMapping> fields in fieldMappings.GroupBy(f => f.CmsField.TemplateField.FieldName))
						{
							FieldMapping field = fields.First();
							if (field.CwbField != null)
							{
								switch (field.CwbField.Type)
								{
									case "choice_radio":
									case "choice_checkbox":
										{
										    foreach (var f in fields)
										    {
										        ItemsRepository.MapChoice(cmsItem, f.CmsField, finalListOptions);
										    }
                                        }
										break;
									case "files":
										{
											ItemsRepository.ResolveAttachmentMapping(cmsItem, field.CmsField);
										}
										break;
									default:
										{
											if (field.CmsField.TemplateField.FieldType == "Datetime" || field.CmsField.TemplateField.FieldType == "Date")
											{
												ItemsRepository.MapDateTime(cmsItem, field.CmsField);
											}
											else
											{
												if (fields.Count() > 1)
												{
													field.CmsField.Value = string.Join("\r\n", fields.Select(f => f.CmsField.Value.ToString()));
												}

												ItemsRepository.MapText(cmsItem, field.CmsField);
											}
										}
										break;
								}
							}
						}

						var cmsSyncDateField = new CmsField
						{
							TemplateField = new CmsTemplateField { FieldName = "Last Sync Date" },
							Value = DateTime.UtcNow.ToString("yyyyMMddTHHmmss")
						};

						ItemsRepository.MapText(cmsItem, cmsSyncDateField);
					}

				}
				else
				{
					//no template mapping, set error message
					itemResponseModel.ImportMessage = "Update failed: Template not mapped";
					itemResponseModel.IsImportSuccessful = false;
				}
				model.Add(itemResponseModel);

			}

			return model;
		}

		private bool CheckFieldError(TemplateMapping templateMapping, List<Element> cwbFields, List<File> files, ItemResultModel itemResponseModel)
		{
			bool fieldError = false;

			var groupedFields = templateMapping.FieldMappings.GroupBy(i => i.CmsField);

			foreach (var grouping in groupedFields)
			{
				CmsField cmsField = grouping.Key;

				var cwbFieldIds = grouping.Select(i => i.CwbField.Id);
				var cwbFieldsToMap = grouping.Select(i => i.CwbField);

				IEnumerable<Element> cwbFieldsForMapping =
					cwbFields.Where(i => cwbFieldIds.Contains(i.Name)).ToList();

				var cwbField = cwbFieldsForMapping.FirstOrDefault();

				if (cwbField != null)
				{
					var value = GetValue(cwbFieldsForMapping);
					var options = GetOptions(cwbFieldsForMapping);

					cmsField.Files = files.Where(x => x.FieldId == cwbField.Name).ToList();
					cmsField.Value = value;
					cmsField.Options = options;

					//update CWB fields' type
					foreach (var field in cwbFieldsToMap)
					{
						field.Type = cwbField.Type;
					}
				}
				else
				{
					//if field error, set error message
					itemResponseModel.ImportMessage = "Update failed: Template fields mismatch";
					itemResponseModel.IsImportSuccessful = false;
					fieldError = true;
					break;
				}
			}
			return fieldError;
		}
	}
}
