using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.Update;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class UpdateResultModel
    {
        public List<UpdateItemResponseModel> Items { get; set; }

        public UpdateResultModel(List<UpdateItemResponseModel> items)
        {
            Items = items;
        }
    }
}
