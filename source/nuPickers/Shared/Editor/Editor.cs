namespace nuPickers.Shared.Editor
{
	using DataSource;
	using nuPickers.Shared.CustomLabel;
	using nuPickers.Shared.TypeaheadListPicker;
	using Paging;
	using System.Collections.Generic;
	using System.Linq;
	using Umbraco.Core;
	using Umbraco.Core.Models;
	using Umbraco.Web;
	using System;

	internal static class Editor
    {

		private static UmbracoHelper uh = null;

        /// <summary>
        /// Get a collection of all the (key/label) items for a picker (with optional typeahead)
        /// </summary>
        /// <param name="currentId">the current id</param>
        /// <param name="parentId">the parent id</param>
        /// <param name="propertyAlias">the property alias</param>
        /// <param name="dataSource">the datasource</param>
        /// <param name="customLabelMacro">an optional macro to use for custom labels</param>
        /// <param name="typeahead">optional typeahead text to filter down on items returned</param>
        /// <returns>a collection of <see cref="EditorDataItem"/></returns>
        internal static IEnumerable<EditorDataItem> GetEditorDataItems(                                                        
                                                        int currentId,
                                                        int parentId,
                                                        string propertyAlias,
														string docTypeAlias, // S6 Added docTypeAlias
                                                        IDataSource dataSource, 
                                                        string customLabelMacro,
                                                        string typeahead = null)
        {
            IEnumerable<EditorDataItem> editorDataItems = Enumerable.Empty<EditorDataItem>(); // default return data

            if (dataSource != null)
            {
                editorDataItems = dataSource.GetEditorDataItems(currentId, parentId, typeahead); // both are passed as current id may = 0 (new content)
				//string docTypeAlias = GetDocumentTypeAlias(parentId);
                if (!string.IsNullOrWhiteSpace(customLabelMacro))
                {
					int docTypeId = -1;
					uh = new UmbracoHelper(UmbracoContext.Current);
					IPublishedContent node = null;

					if (currentId > 0)
					{						
						node = uh.TypedContent(currentId);						
					} else
					{
						node = uh.TypedContent(parentId);
					}

					if (node != null)
					{
						docTypeId = node.DocumentTypeId;
					}					

					editorDataItems = new CustomLabel(customLabelMacro, currentId, propertyAlias, parentId, docTypeAlias).ProcessEditorDataItems(editorDataItems);
                }

                // if the datasource didn't handle the typeahead text, then it needs to be done here (post custom label processing ?)
                if (!dataSource.HandledTypeahead && !string.IsNullOrWhiteSpace(typeahead))
                {
                    editorDataItems = new TypeaheadListPicker(typeahead).ProcessEditorDataItems(editorDataItems);
                }
            }

            return editorDataItems;
        }

		// S6 Helper method to determine the ("likely") document type of a new entity content node
		private static string GetDocumentTypeAlias(int parentId)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get a collection of the picked (key/label) items
		/// </summary>
		/// <param name="currentId">the current id</param>
		/// <param name="parentId">the parent id</param>
		/// <param name="propertyAlias">the property alias</param>
		/// <param name="dataSource">the datasource</param>
		/// <param name="customLabelMacro">an optional macro to use for custom labels</param>
		/// <returns>a collection of <see cref="EditorDataItem"/></returns>
		internal static IEnumerable<EditorDataItem> GetEditorDataItems(
                                                        int currentId,
                                                        int parentId,
                                                        string propertyAlias,
														string docTypeAlias, // S6 Added docTypeAlias
														IDataSource dataSource,
                                                        string customLabelMacro,
                                                        string[] keys)

        {
            IEnumerable<EditorDataItem> editorDataItems = Enumerable.Empty<EditorDataItem>(); // default return data

            if (dataSource != null)
            {
                editorDataItems = dataSource.GetEditorDataItems(currentId, parentId, keys);

                if (!string.IsNullOrWhiteSpace(customLabelMacro))
                {
                    editorDataItems = new CustomLabel(customLabelMacro, currentId, propertyAlias, parentId, docTypeAlias).ProcessEditorDataItems(editorDataItems);
                }

                // ensure sort order matches order of keys supplied
                editorDataItems = editorDataItems.OrderBy(x => keys.IndexOf(x.Key));
            }

            return editorDataItems;
        }

        /// <summary>
        /// Get a page of (key/label) items for a picker
        /// </summary>
        /// <param name="currentId">the current id</param>
        /// <param name="parentId">the parent id</param>
        /// <param name="propertyAlias">the property alias</param>
        /// <param name="dataSource">the datasource</param>
        /// <param name="customLabelMacro">an optional macro to use for custom labels</param>
        /// <param name="itemsPerPage">number of items per page</param>
        /// <param name="page">the page of (key/label) items to get</param>
        /// <returns>a collection of <see cref="EditorDataItem"/></returns>
        internal static IEnumerable<EditorDataItem> GetEditorDataItems(
                                                int currentId,
                                                int parentId,
                                                string propertyAlias,
												string docTypeAlias, // S6 Added docTypeAlias
												IDataSource dataSource,
                                                string customLabelMacro,
                                                int itemsPerPage,
                                                int page,
                                                out int total)
        {
            IEnumerable<EditorDataItem> editorDataItems = Enumerable.Empty<EditorDataItem>(); // default return data
            total = -1;

            if (dataSource != null)
            {
                editorDataItems = dataSource.GetEditorDataItems(
                                                currentId, 
                                                parentId, 
                                                new PageMarker(itemsPerPage, page), 
                                                out total);

                if (!string.IsNullOrWhiteSpace(customLabelMacro))
                {
                    editorDataItems = new CustomLabel(customLabelMacro, currentId, propertyAlias, parentId, docTypeAlias).ProcessEditorDataItems(editorDataItems);
                }
            }

            return editorDataItems;
        }
    }
}