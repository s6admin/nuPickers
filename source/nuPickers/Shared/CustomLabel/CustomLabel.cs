﻿
namespace nuPickers.Shared.CustomLabel
{
    using nuPickers.Shared.Editor;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using umbraco;
    using umbraco.NodeFactory;
    using umbraco.presentation.templateControls;

    internal class CustomLabel
    {
        private string MacroAlias { get; set; }

        /// <summary>
        /// return true when there is a published page anywhere on the site
        /// </summary>
        [DefaultValue(false)]
        private bool HasMacroContext { get; set; }

        private int ContextId { get; set; }

        private string PropertyAlias { get; set; }

		// S6
		private int DocTypeId { get; set; }
		private string DocTypeAlias { get; set; }
		private int ParentId { get; set; }		

        /// <summary>
        /// 
        /// </summary>
        /// <param name="macroAlias">alias of Macro to execute</param>
        /// <param name="contextId">node, media or member id</param>
        /// <param name="propertyAlias">property alias</param>
        internal CustomLabel(string macroAlias, int contextId, string propertyAlias, int parentId, string docTypeAlias = "")
        {
            this.MacroAlias = macroAlias;
            this.ContextId = contextId;
            this.PropertyAlias = propertyAlias;
			//this.DocTypeId = docTypeId;
			this.DocTypeAlias = docTypeAlias;
			this.ParentId = parentId;

            // the macro requires a published context to run in
            Node currentNode = uQuery.GetNode(contextId);
            if (currentNode != null)
            {
                // current page is published so use this as the macro context
                HttpContext.Current.Items["pageID"] = contextId;
                this.HasMacroContext = true;
            }
            else
            {
                 // fallback nd find first published page to use as host
                 Node contextNode = uQuery.GetNodesByXPath(string.Concat("descendant::*[@parentID = ", uQuery.RootNodeId, "]")).FirstOrDefault();
                 if (contextNode != null)
                 {
                     HttpContext.Current.Items["pageID"] = contextNode.Id;
                     this.HasMacroContext = true;
                 }
            }

        }

        /// <summary>
        /// parses the collection of options, potentially transforming the content of the label
        /// </summary>
        /// <param name="contextId">the content / media or member being edited</param>
        /// <param name="editorDataItems">collection of options</param>
        /// <returns></returns>
        internal IEnumerable<EditorDataItem> ProcessEditorDataItems(IEnumerable<EditorDataItem> editorDataItems)
        {
            string keys = string.Join(", ", editorDataItems.Select(x => x.Key)); // csv of all keys
            int counter = 0;
            int total = editorDataItems.Count();

            foreach (EditorDataItem editorDataItem in editorDataItems)
            {
                counter++;
                editorDataItem.Label = this.ProcessMacro(editorDataItem.Key, editorDataItem.Label, keys, counter, total);
            }

            return editorDataItems.Where(x => !string.IsNullOrWhiteSpace(x.Label)); // remove any options without a label
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">passed by parameter into the macro</param>
        /// <param name="label">value to return if macro fails</param>
        /// <param name="keys">csv of all keys</param>
        /// <param name="counter">current postion</param>
        /// <param name="total">total number of keys</param>
        /// <returns>the output of the macro as a string</returns>
        private string ProcessMacro(string key, string label, string keys, int counter, int total)
        {
            if (!string.IsNullOrWhiteSpace(this.MacroAlias) && this.HasMacroContext)
            {
                Macro macro = new Macro() { Alias = this.MacroAlias };

                macro.MacroAttributes.Add("contextId".ToLower(), this.ContextId);
                macro.MacroAttributes.Add("propertyAlias".ToLower(), this.PropertyAlias);

                macro.MacroAttributes.Add("key", key);
                macro.MacroAttributes.Add("label", label);

                macro.MacroAttributes.Add("keys", keys);
                macro.MacroAttributes.Add("counter", counter);
                macro.MacroAttributes.Add("total", total);

				// S6
				//macro.MacroAttributes.Add("docTypeId".ToLower(), this.DocTypeId);
				macro.MacroAttributes.Add("docTypeAlias".ToLower(), this.DocTypeAlias);
				macro.MacroAttributes.Add("parentId".ToLower(), this.ParentId);

                label = this.RenderToString(macro);
            }

            return label;
        }

        /// <summary>
        /// Method added here to remove the need for the more generic ControlExtensions (as unlikely to need this functionality elsewhere)
        /// </summary>
        /// <param name="macro"></param>
        private string RenderToString(Macro macro)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(stringBuilder))
            using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(stringWriter))
            {
                macro.RenderControl(htmlTextWriter);
            }

            return stringBuilder.ToString();
        }
    }
}
