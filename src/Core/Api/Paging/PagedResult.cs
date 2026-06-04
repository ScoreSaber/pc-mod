using System.Collections.Generic;

namespace ScoreSaber.Core.Api.Paging {
    internal class PageMetadata {
        internal int Page { get; set; }
        internal int ItemsPerPage { get; set; }
        internal int TotalItems { get; set; }
        internal int TotalPages { get; set; }
    }

    internal class PagedResult<T> {
        internal List<T> Items { get; set; } = new List<T>();
        internal PageMetadata Metadata { get; set; } = new PageMetadata();
    }
}
