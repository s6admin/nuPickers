﻿namespace nuPickers.PropertyEditors.EnumPrefetchListPicker
{
    using nuPickers.EmbeddedResource;
    using Umbraco.Core.PropertyEditors;

    internal class EnumPrefetchListPickerPreValueEditor : PreValueEditor
    {
        [PreValueField("dataSource", "", EmbeddedResource.ROOT_URL + "EnumDataSource/EnumDataSourceConfig.html", HideLabel = true)]
        public string DataSource { get; set; }

        [PreValueField("customLabel", "", EmbeddedResource.ROOT_URL + "CustomLabel/CustomLabelConfig.html", HideLabel = true)]
        public string CustomLabel { get; set; }

        [PreValueField("prefetchListPicker", "", EmbeddedResource.ROOT_URL + "PrefetchListPicker/PrefetchListPickerConfig.html", HideLabel = true)]
        public string PrefetchListPicker { get; set; }

        [PreValueField("listPicker", "", EmbeddedResource.ROOT_URL + "ListPicker/ListPickerConfig.html", HideLabel = true)]
        public string ListPicker { get; set; }

        [PreValueField("saveFormat", "Save Format", EmbeddedResource.ROOT_URL + "SaveFormat/SaveFormatConfig.html")]
        public string SaveFormat { get; set; }
    }
}