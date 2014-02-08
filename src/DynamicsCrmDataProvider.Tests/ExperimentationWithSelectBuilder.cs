﻿using System;
using System.Configuration;
using System.Data;
using System.Linq;
using DynamicsCrmDataProvider.Dynamics;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NUnit.Core;
using NUnit.Framework;
using Rhino.Mocks;
using SQLGeneration.Builders;
using SQLGeneration.Generators;

namespace DynamicsCrmDataProvider.Tests
{
    [TestFixture()]
    public class ExperimentationWithSelectBuilder
    {
        [Test]
        public void TryStuff()
        {
            var commandText = "SELECT * FROM Contact";
            var commandBuilder = new CommandBuilder();
            // commandBuilder.HasSingleSource();
            var command = commandBuilder.GetCommand(commandText);
            var selectBuilder = command as SelectBuilder;

            if (selectBuilder == null)
            {
                throw new InvalidOperationException("Command Text must be a Select statement.");
            }
            if (!selectBuilder.From.Any() || selectBuilder.From.Count() > 1)
            {
                throw new NotSupportedException("You can currently only select from a single entity in a query.");
            }


            var fromTable = (Table)((AliasedSource)selectBuilder.From.First()).Source;
            var firstEntityName = fromTable.Name.ToLower();

            // detect all columns..
            if (!selectBuilder.Projection.Any())
            {
                throw new NotSupportedException("You must include atleast 1 column in the select statement.");
            }

            var query = new QueryExpression(firstEntityName);
            query.ColumnSet = new ColumnSet();
            if (selectBuilder.Projection.Count() == 1)
            {
                var column = selectBuilder.Projection.First().ProjectionItem;
                if (column is AllColumns)
                {
                    query.ColumnSet.AllColumns = true;
                }
                else
                {
                    query.ColumnSet.AddColumn(column.GetProjectionName());
                }
            }
            else
            {
                foreach (var projection in selectBuilder.Projection)
                {
                    query.ColumnSet.AddColumn(projection.ProjectionItem.GetProjectionName());
                }
            }

        }

        [Test]
        public void TryMoreStuff()
        {

            string commandText = "SELECT CustomerId, FirstName, LastName, Created FROM Customer WHERE FirstName IS NOT NULL";
            CommandBuilder commandBuilder = new CommandBuilder();
            SelectBuilder builder = (SelectBuilder)commandBuilder.GetCommand(commandText);
            Formatter formatter = new Formatter();
            commandText = formatter.GetCommandText(builder);
            Console.Write(commandText);

            // Should not have the entity name.

            // for each column
            // selectBuilder.Sources[firstFrom].Column("");
            //var columnSet = new ColumnSet()


            //if(selectBuilder.Sources== null || selectBuilder.Sources.Any())

            //    var customerId = builder.Source["Customer"].Column("CustomerId");
            //    var parameter = new Placeholder("@CustomerId");
            //    var filter = new EqualToFilter(customerId, parameter);
            //    builder.AddWhere(filter);

            //    var formatter = new Formatter();
            //    commandText = formatter.GetCommandText(builder);
        }

        [Category("Integration")]
        [Test]
        public void DeleteThis()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["CrmOrganisation"];
            using (var conn = new CrmDbConnection(connectionString.ConnectionString))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = "SELECT contactid, firstname, lastname FROM Contact WHERE firstname LIKE 'D%'";
                //   command.CommandType = CommandType.Text;
                int resultCount = 0;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultCount++;
                        var contactId = (Guid)reader["contactid"];
                        var firstName = (string)reader["firstname"];
                        var lastName = (string)reader["lastname"];
                        Console.WriteLine(string.Format("{0} {1} {2}", contactId, firstName, lastName));
                    }
                }
                Console.WriteLine("There were " + resultCount + " results..");
            }



        }

    }
}
