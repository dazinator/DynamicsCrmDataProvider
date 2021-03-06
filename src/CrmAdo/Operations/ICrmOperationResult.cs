﻿using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmAdo.Operations
{
    public interface ICrmOperationResult
    {
        OrganizationResponse Response { get; set; }
        ResultSet ResultSet { get; set; }
        int ReturnValue { get; }
        bool UseResultCountAsReturnValue { get; set; }

        bool HasMoreResults { get; }
        void NextOperationResult();

        DbDataReader GetReader(DbConnection connection = null);

    }
}
