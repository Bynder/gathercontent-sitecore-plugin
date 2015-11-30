﻿using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class ImportResultModel
    {
        public List<ImportItemsResponseModel> Items { get; set; }

        public ImportResultModel(List<ImportItemsResponseModel> items)
        {
            Items = items;
        }
    }
}
