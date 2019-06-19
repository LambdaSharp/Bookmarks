using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using LambdaSharp.Challenge.Bookmarker.Shared;

namespace LambdaSharp.Challenge.Bookmarker.ApiFunctions {

    public class AddBookmarkRequest {

        //--- Properties ---
        [JsonRequired]
        public string Url { get; set; }
    }

    public class AddBookmarkResponse {

        //--- Properties ---
        [JsonRequired]
        public string ID { get; set; }
    }

    public class GetBookmarkResponse : Bookmark{}

    public class GetBookmarksResponse {

        //--- Properties ---
        [JsonRequired]
        public List<Bookmark> Bookmarks = new List<Bookmark>();
    }

    public class DeleteBookmarkResponse {

        //--- Properties ---
        [JsonRequired]
        public bool Deleted;
    }
}
