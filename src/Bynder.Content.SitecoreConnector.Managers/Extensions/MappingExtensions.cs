namespace Bynder.Content.SitecoreConnector.Managers.Extensions
{
    using Core.Models.Mapping;
    using Models.Mapping;

    public static class MappingExtensions
    {
        public static MappingModel CastToMappingModel(this TemplateMapping templateMapping)
        {
            var mappingModel = new MappingModel
            {
                CwbProject = new CwbProjectModel
                {
                    Name = templateMapping.CwbProjectName
                },
                CwbTemplate = new CwbTemplateModel
                {
                    Id = templateMapping.CwbTemplate.CwbTemplateId,
                    Name = templateMapping.CwbTemplate.CwbTemplateName,
                },
                CmsTemplate = new CmsTemplateModel
                {
                    Name = templateMapping.CmsTemplate.TemplateName,
                },
                MappingId = templateMapping.MappingId,
                MappingTitle = templateMapping.MappingTitle,
                LastMappedDateTime = templateMapping.LastMappedDateTime,
                LastUpdatedDate = templateMapping.LastUpdatedDate,
                MappingGroupId = templateMapping.MappingGroupId,
                IsRelated = templateMapping.IsRelated
            };

            return mappingModel;
        }
    }
}
