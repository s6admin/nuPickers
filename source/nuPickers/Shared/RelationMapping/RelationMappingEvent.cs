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
		
		// Picker Lists that have changed selections, grouped by each entity being saved
		private Dictionary<Guid, List<Picker>> dirtyPickers;

		protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{

			ContentService.Saving += this.ContentService_Saving;
			MediaService.Saving += this.MediaService_Saving;
			MemberService.Saving += this.MemberService_Saving;

			ContentService.Saved += ContentService_Saved;			
			MediaService.Saved += MediaService_Saved;
			MemberService.Saved += MemberService_Saved;

			// NOTE: all relations to an id are automatically deleted when emptying the recycle bin
		}
		
		private void ContentService_Saving(IContentService sender, SaveEventArgs<IContent> e)
		{
			this.Saving((IService)sender, e.SavedEntities);
		}

		private void MediaService_Saving(IMediaService sender, SaveEventArgs<IMedia> e)
		{
			this.Saving((IService)sender, e.SavedEntities);
		}

		private void MemberService_Saving(IMemberService sender, SaveEventArgs<IMember> e)
		{
			this.Saving((IService)sender, e.SavedEntities);
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
		/// Create/update any relations for pickers and nullify their stored values in RelationsOnly is the selected saveFormat
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="changedEntities"></param>
		private void Saving(IService sender, IEnumerable<IContentBase> changedEntities)
		{

			dirtyPickers = new Dictionary<Guid, List<Picker>>();
						
			foreach (IContentBase entity in changedEntities)
			{

				if (entity != null && entity.IsDirty())
				{
										
					List<Picker> dirtyEntityPickers = new List<Picker>();

					// Loop all Pickers in saved Content item
					foreach (PropertyType propertyType in entity.PropertyTypes.Where(x => PickerPropertyValueConverter.IsPicker(x.PropertyEditorAlias)))
					{

						/* S6 Note: NULL or empty collection values in nuPicker aren't being flagged as Dirty so ALWAYS check each picker control
							and determine "dirty" status based on the property value
						*/
						//if (entity.IsPropertyDirty(propertyType.Alias))
						//{

							Picker picker = new Picker(
											   entity.Id,
											   entity.ParentId,
											   propertyType.Alias,
											   propertyType.DataTypeDefinitionId,
											   propertyType.PropertyEditorAlias,
											   entity.GetValue(propertyType.Alias));
							
							if (!string.IsNullOrWhiteSpace(picker.RelationTypeAlias))
							{
															
								// Using the ContentService.Save() to update a node will delete all existing relations. https://github.com/uComponents/nuPickers/issues/105
								bool isRelationsOnly = picker.GetDataTypePreValue("saveFormat").Value == "relationsOnly";
								if (isRelationsOnly)
								{

									if (picker.SavedValue == null)
									{
										/* S6 TODO
											
											The nuPicker library needs a way to distinguish between a property editor being truly empty (due to an admin
											removing relation items in the control) versus a naturally dirty editor (ie. UpdateDate change?) having its
											valid existing relations aggressively deleted because the PickedKeys is coincidentally empty.
											
											1. Conceptually, if the RelationsOnly Picker control retrieved its pickedKeys on construction/render and stored
											the values in a second temporary property (ie. PrevPickedKeys) it could be compared to the PickedKeys 
											property directly when the parent node is saved to determine the change in selection. That basically sounds like
											how any Umbraco data is compared within the cache.
											
											2. Another option could be to explicitly use the PickedKeys NULL value (make no changes) versus an empty collection
											implying the admin has removed items and the database Relations SHOULD be cleared.

											Its possible, though unclear, that retrieving the Cache or DB values each time the PickedKeys property is
											referenced (its WITHIN the PickedKeys property getter) is causing a problem.

											Either:
											1. DON'T set an empty collection for PickedKeys here 
											or
											2. ADD a check to Picker.cs PickedKeys that also includes an empty collection to fallback to the database

											

										*/
										picker.PickedKeys = new string[] { };  // Leave NULL as NULL...use it to indicate no relation changes should be made
										//continue; // Skip NULL nuPicker editors so valid existing relations aren't wiped from the database
									}
									else
									{
										// manually set on picker obj, so it doesn't then attempt to read picked keys from the database
										picker.PickedKeys = SaveFormat.GetKeys(picker.SavedValue.ToString()).ToArray();
																				
										// delete saved value (setting it to null)
										entity.SetValue(propertyType.Alias, null);										
									}

								}

								dirtyEntityPickers.Add(picker); // Retain any relation-mapped Picker(s) for Save event													
							}
						//}
					}

					if (dirtyEntityPickers.Any())
					{
						dirtyPickers.Add(entity.Key, dirtyEntityPickers);
					}					
				}
			}
		}

		private void Saved(IService sender, IEnumerable<IContentBase> changedEntities)
		{
			foreach (IContentBase entity in changedEntities)
			{
				// Loop Picker editors for current saved entity
				Picker dirtyPicker = null;

				foreach (PropertyType propertyType in entity.PropertyTypes.Where(x => PickerPropertyValueConverter.IsPicker(x.PropertyEditorAlias)))
				{						
					try
					{
						dirtyPicker = this.dirtyPickers[entity.Key].FirstOrDefault(x => x.PropertyAlias == propertyType.Alias);
					} catch(Exception ex)
					{
						// TODO Handle as seen fit
						Console.WriteLine(ex.Message);
					}						

					// If no dirtyPicker was found selections are the same, proceed to next iteration
					if (dirtyPicker == null)
					{
						continue;
					}

					bool isRelationsOnly = dirtyPicker.GetDataTypePreValue("saveFormat").Value == "relationsOnly";

					RelationMapping.UpdateRelationMapping(
						entity.Id,
						dirtyPicker.PropertyAlias,
						dirtyPicker.RelationTypeAlias,
						isRelationsOnly,
						dirtyPicker.PickedIds.ToArray());
											
				}
			}
			dirtyPickers = null;
		}		
	}
}