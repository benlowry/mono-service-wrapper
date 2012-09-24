using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ServiceWrapper
{
    public class DataStore
    {
        public static IEnumerable<BsonDocument> ServiceList()
        {
            try
            {
                var server = MongoServer.Create(ConfigurationManager.ConnectionStrings["mongodb"].ConnectionString);
                server.Connect();

                var database = server.GetDatabase("config");
                var collection = database.GetCollection("services");

                var services = collection.FindAll();
                server.Disconnect();

                return services.ToList();
            }
            catch(Exception err)
            {
                Console.WriteLine("** error loading service list: " + err.Message);
            }

            return new List<BsonDocument>();
        }

        public static bool GetStatus(BsonDocument service)
        {
            try
            {
                var server = MongoServer.Create(ConfigurationManager.ConnectionStrings["mongodb"].ConnectionString);
                server.Connect();

                var database = server.GetDatabase("config");
                var collection = database.GetCollection("services");
                var filter = new QueryDocument { { "_id", service["_id"].AsObjectId } };
                var doc = collection.FindOne(filter);
                var restart = doc != null && doc.Contains("restart") && doc["restart"].AsBoolean;
                server.Disconnect();

                return restart;
            }
            catch (Exception err)
            {
                Console.WriteLine("** error checking restart status: " + err.Message);
                return false;
            }
        }

        public static void SetStatus(BsonDocument service, bool restart)
        {
            try
            {
                var server = MongoServer.Create(ConfigurationManager.ConnectionStrings["mongodb"].ConnectionString);
                server.Connect();

                var database = server.GetDatabase("config");
                var collection = database.GetCollection("services");
                var filter = new QueryDocument { { "_id", service["_id"].AsObjectId } };
                var update = new UpdateDocument();

                foreach (var key in service)
                    update[key.Name] = key.Value;

                update["restart"] = restart;
                collection.Update(filter, update);

                server.Disconnect();
            }
            catch (Exception err)
            {
                Console.WriteLine("** error updating restart status: " + err.Message);
            }
        }
    }
}
