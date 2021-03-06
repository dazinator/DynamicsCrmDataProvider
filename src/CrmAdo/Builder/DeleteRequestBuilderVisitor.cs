﻿using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using SQLGeneration.Builders;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrmAdo.Dynamics;
using Microsoft.Xrm.Sdk;
using CrmAdo.Operations;
using CrmAdo.Core;

namespace CrmAdo.Visitor
{
    /// <summary>
    /// A <see cref="BuilderVisitor"/> that builds a <see cref="DeleteRequest"/> when it visits an <see cref="DeleteBuilder"/> 
    /// </summary>
    public class DeleteRequestBuilderVisitor : BaseOrganizationRequestBuilderVisitor<DeleteRequest>
    {

        public DeleteRequestBuilderVisitor(ICrmMetaDataProvider metadataProvider, ConnectionSettings settings)
            : this(null, metadataProvider, new DynamicsAttributeTypeProvider(), settings)
        {

        }

        public DeleteRequestBuilderVisitor(DbParameterCollection parameters, ICrmMetaDataProvider metadataProvider, IDynamicsAttributeTypeProvider typeProvider, ConnectionSettings settings)
            : base(metadataProvider, settings)
        {
            //Request = new DeleteRequest();
            Parameters = parameters;
           // MetadataProvider = metadataProvider;
            IsVisitingRightFilterItem = false;
            DynamicsTypeProvider = typeProvider;
        }



        private IDynamicsAttributeTypeProvider DynamicsTypeProvider { get; set; }
       // public DeleteRequest Request { get; set; }

        public DbParameterCollection Parameters { get; set; }
       // private ICrmMetaDataProvider MetadataProvider { get; set; }

        private string EntityName { get; set; }
        private EqualToFilter EqualToFilter { get; set; }
        private bool IsVisitingRightFilterItem { get; set; }
        private bool IsVisitingFilterItem { get; set; }

        private Column IdFilterColumn { get; set; }
        private object IdFilterValue { get; set; }

        #region Visit Methods

        protected override void VisitDelete(DeleteBuilder item)
        {
            GuardDeleteBuilder(item);
            item.Table.Source.Accept(this);
            int whereCount = 0;
            foreach (IVisitableBuilder where in item.Where)
            {
                where.Accept(this);
                whereCount++;
            }
            if (whereCount != 1)
            {
                throw new ArgumentException("The delete statement should have a single filter in the where clause, which should specify the entity id of the record to be deleted.");
            }
            if (EqualToFilter == null)
            {
                throw new NotSupportedException("The delete statement has an unsupported filter in it's where clause. The where clause should contain a single 'equal to' filter that specifies the entity id of the particular record to delete.");
            }
            if (IdFilterColumn == null)
            {
                throw new NotSupportedException("The delete statement has an unsupported filter in it's where clause. The'equal to' filter should specify the entity id column on one side.");
            }
            var idAttName = GetColumnLogicalAttributeName(IdFilterColumn);

            var expectedIdAttributeName = string.Format("{0}id", EntityName.ToLower());
            if (idAttName != expectedIdAttributeName)
            {
                throw new NotSupportedException("The delete statement has an unsupported filter in it's where clause. The'equal to' filter should specify the id column of the entity on one side.");
            }

            EntityReference entRef = new EntityReference();
            entRef.LogicalName = EntityName;
            entRef.Id = DynamicsTypeProvider.GetUniqueIdentifier(IdFilterValue);
            CurrentRequest.Target = entRef;

        }

        protected override void VisitEqualToFilter(EqualToFilter item)
        {
            EqualToFilter = item;
            IsVisitingFilterItem = true;
            item.LeftHand.Accept(this);
            IsVisitingRightFilterItem = true;
            item.RightHand.Accept(this);
            IsVisitingRightFilterItem = false;
            IsVisitingFilterItem = false;
        }

        protected override void VisitTable(Table item)
        {
            EntityName = GetTableLogicalEntityName(item);
        }

        protected override void VisitColumn(Column item)
        {
            if (IsVisitingFilterItem)
            {
                IdFilterColumn = item;
            }
        }

        protected override void VisitStringLiteral(StringLiteral item)
        {
            var sqlValue = ParseStringLiteralValue(item);
            if (IsVisitingFilterItem)
            {
                IdFilterValue = sqlValue;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        protected override void VisitNumericLiteral(NumericLiteral item)
        {
            var sqlValue = ParseNumericLiteralValue(item);
            if (IsVisitingFilterItem)
            {
                IdFilterValue = sqlValue;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        protected override void VisitNullLiteral(NullLiteral item)
        {
            if (IsVisitingFilterItem)
            {
                IdFilterValue = null;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        protected override void VisitPlaceholder(Placeholder item)
        {
            bool hasParam;
            var paramVal = GetParamaterValue(item.Value, out hasParam);
            if (IsVisitingFilterItem && hasParam)
            {
                IdFilterValue = paramVal;
            }
            else
            {
                throw new NotSupportedException();
            }

        }

        protected override void VisitFilterGroup(FilterGroup item)
        {
            foreach (var filter in item.Filters)
            {
                filter.Accept(this);
            }           
        }

        #endregion

        private void GuardDeleteBuilder(DeleteBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (builder.Table == null)
            {
                throw new ArgumentException("The delete statement must specify a single table name to update (this is the logical name of the entity).");
            }
        }

        private object GetParamaterValue(string paramName, out bool paramFound)
        {
            if (Parameters.Contains(paramName))
            {
                paramFound = true;
                var param = Parameters[paramName];
                return param.Value;
                // throw new InvalidOperationException("Missing parameter value for parameter named: " + paramName);
            }
            paramFound = false;
            return null;

        }

        public override ICrmOperation GetCommand()
        {
            var orgCommand = new DeleteOperation(ResultColumnMetadata, Request);
            return orgCommand;
        }

    }
}
