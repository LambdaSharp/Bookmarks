using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.APIGatewayEvents;
using OpenGraphNet;
using LambdaSharp.ApiGateway;
using LambdaSharp.Challenge.Bookmarker.Shared;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.Challenge.Bookmarker.ApiFunctions {

    public class Function : ALambdaApiGatewayFunction {

        //--- Fields ---
        private IAmazonDynamoDB _dynamoDbClient;
        private Table _table;

        public override async Task InitializeAsync(LambdaConfig config) {
            // initialize AWS clients
            _dynamoDbClient = new AmazonDynamoDBClient();

            // read settings
            _table = Table.LoadTable(_dynamoDbClient, config.ReadDynamoDBTableName("BookmarksTable"));
        }

        public AddBookmarkResponse AddBookmark(AddBookmarkRequest request) {
            LogInfo($"Add Bookmark:  Url={request.Url}");
            Uri url;
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out url)) AbortBadRequest("Url Not Valid");

            // Level 1: generate a short ID that is still unique
            var id = Guid.NewGuid().ToString("D");
            var bookmark = new Bookmark {
                ID = id,
                Url = url,
            };
            _table.PutItemAsync(Document.FromJson(SerializeJson(bookmark))).Wait();
            return new AddBookmarkResponse{
                ID = bookmark.ID
            };
         }
        public GetBookmarkResponse GetBookmark(string id) {
            LogInfo($"Get Bookmark: ID={id}");
            var bookmark = RetrieveBookmark(id) ?? throw AbortNotFound("Bookmark not found");
            return new GetBookmarkResponse{
                ID = bookmark.ID,
                Url = bookmark.Url,
                Title = bookmark.Title,
                Description = bookmark.Description,
                ImageUrl = bookmark.ImageUrl,
            };
        }

        public GetBookmarksResponse GetBookmarks(string contains = null, int offset = 0, int limit = 10) {
            var search = _table.Scan(new ScanFilter());
            var bookmarks = new List<Bookmark>();
            do {
                var documentList = search.GetNextSetAsync().Result;
                foreach (var document in documentList)
                    bookmarks.Add(DeserializeJson<Bookmark>(document.ToJson()));
            } while (!search.IsDone);
            return new GetBookmarksResponse{
                Bookmarks = bookmarks
            };
        }

        public DeleteBookmarkResponse DeleteBookmark(string id) {
            LogInfo($"Delete Bookmark: ID={id}");
            _table.DeleteItemAsync(id).Wait();
            return new DeleteBookmarkResponse{
                Deleted = true,
            };
        }

        public APIGatewayProxyResponse GetBookmarkPreview(string id) {
            LogInfo($"Get Bookmark Preview: ID={id}");
            var bookmark = RetrieveBookmark(id) ?? throw AbortNotFound("Bookmark not found");

            var html = $@"<html>
<head>
    <title>{WebUtility.HtmlEncode(bookmark.Title)}</title>
</head>
<body style=""font-family: Helvetica, Arial, sans-serif;"">
    <h1>{WebUtility.HtmlEncode(bookmark.Title)}</h1>
</body>
</html>
";
            return new APIGatewayProxyResponse{
                Body = html,
                StatusCode = 200,
                Headers = new Dictionary<string,string>(){
                    {"Content-Type", "text/html"},
                },
            };
        }

        private Bookmark RetrieveBookmark(string id) {
            var document = _table.GetItemAsync(id).Result;
            return (document == null)
                ? null
                : DeserializeJson<Bookmark>(document.ToJson());
        }
    }
}
