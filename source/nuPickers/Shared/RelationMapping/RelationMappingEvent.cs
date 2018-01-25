namespace nuPickers.Shared.RelationMapping
{
	using nuPickers.Shared.SaveFormat;
	using System.Collections.Generic;
	using System.Linq;
	using Umbraco.Core;
	using Umbraco.Core.Events;
	using Umbraco.Core.Models;
	using Umbraco.Core.Services;
	using System;

	/// <summary>
	/// server side event to update relations on change of any content / media / member using a nuPicker with relation mapping
	/// </summary>
	public class RelationMappingEvent : ApplicationEventHandler
    {

		// S6 Saves a placeholder value when the 
		//private const string RELATIONS_ONLY_TOKEN = "relationsOnlyToken";

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ContentService.Saved += this.ContentService_Saved;
            MediaService.Saved += this.MediaService_Saved;
            MemberService.Saved += this.MemberService_Saved;

			// S6 Added
			ContentService.Saving += this.ContentService_Saving;
            
            // NOTE: all relations to an id are automatically deleted when emptying the recycle bin
        }

		// S6
		private void ContentService_Saving(IContentService sender, SaveEventArgs<IContent> e)
		{
			
			foreach (IContent savedEntity in e.SavedEntities)
			{
				if(savedEntity != null)
				{
					// Loop all Pickers in saved Content item
					foreach (PropertyType propertyType in savedEntity.PropertyTypes.Where(x => PickerPropertyValueConverter.IsPicker(x.PropertyEditorAlias)))
					{
						Picker picker = new Picker(
										   savedEntity.Id,
										   savedEntity.ParentId,
										   propertyType.Alias,
										   propertyType.DataTypeDefinitionId,
										   propertyType.PropertyEditorAlias,
										   savedEntity.GetValue(propertyType.Alias));

						if (!string.IsNullOrWhiteSpace(picker.RelationTypeAlias))
						{
							bool isRelationsOnly = picker.GetDataTypePreValue("saveFormat").Value == "relationsOnly";

							if (isRelationsOnly)
							{
								//savedEntity.SetValue(propertyType.Alias, "s6RelationsOnly");								
							}
						}
					}					
				}				
			}
		}

		private void ContentService_Saved(IContentService sender, SaveEventArgs<IContent> e)
        {
            this.Saved((IService)sender, e.SavedEntities);
        }

        private void MediaService_Saved(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            this.Saved((IService)sender, e.SavedEntities);
        }

        private void MemberService_Saved(IMemberService sender, SaveEventArgs<IMember> e)
        {
            this.Saved((IService)sender, e.SavedEntities);
        }

        /// <summary>
        /// combined event for content / media / member 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="savedEntities"></param>
        private void Saved(IService sender, IEnumerable<IContentBase> savedEntities)
        {
            foreach (IContentBase savedEntity in savedEntities)
            {
                // for each property
                foreach (PropertyType propertyType in savedEntity.PropertyTypes.Where(x => PickerPropertyValueConverter.IsPicker(x.PropertyEditorAlias)))
                {
                    // create picker supplying all values
                    Picker picker = new Picker(
                                            savedEntity.Id, 
                                            savedEntity.ParentId,
                                            propertyType.Alias, 
                                            propertyType.DataTypeDefinitionId, 
                                            propertyType.PropertyEditorAlias,
                                            savedEntity.GetValue(propertyType.Alias));

                    if (!string.IsNullOrWhiteSpace(picker.RelationTypeAlias))
                    {
                        bool isRelationsOnly = picker.GetDataTypePreValue("saveFormat").Value == "relationsOnly";

                        if (isRelationsOnly) 
                        {

							// S6 TODO If Relations Only is true, always save a placeholder value to work-around Umbraco unpublishing
														
                            if (picker.SavedValue == null)
                            {
                                picker.PickedKeys = new string[] { };
                            }
                            else
                            {
                                // manually set on picker obj, so it doesn't then attempt to read picked keys from the database
                                picker.PickedKeys = SaveFormat.GetKeys(picker.SavedValue.ToString()).ToArray();

								// delete saved value (setting it to null)
								//savedEntity.SetValue(propertyType.Alias, null); // S6 No, changing a property value here is what marks the Content as Dirty and is likely where the Publishing bug originates
								
								if (sender is IContentService)
                                {
                                    ((IContentService)sender).Save((IContent)savedEntity, 0, false);
                                }
                                else if (sender is IMediaService)
                                {
                                    ((IMediaService)sender).Save((IMedia)savedEntity, 0, false);
                                }
                                else if (sender is IMemberService)
                                {
                                    ((IMemberService)sender).Save((IMember)savedEntity, false);
                                }
                            }
                        }
                       
                        // update database
                        RelationMapping.UpdateRelationMapping(
                                                picker.ContextId,           // savedEntity.Id
                                                picker.PropertyAlias,       // propertyType.Alias
                                                picker.RelationTypeAlias,
                                                isRelationsOnly,
                                                picker.PickedIds.ToArray());
                    }
                }
            }
        }
    }
}