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

						if (entity.IsPropertyDirty(propertyType.Alias))
						{

							Picker picker = new Picker(
											   entity.Id,
											   entity.ParentId,
											   propertyType.Alias,
											   propertyType.DataTypeDefinitionId,
											   propertyType.PropertyEditorAlias,
											   entity.GetValue(propertyType.Alias));
							
							if (!string.IsNullOrWhiteSpace(picker.RelationTypeAlias))
							{

								bool isRelationsOnly = picker.GetDataTypePreValue("saveFormat").Value == "relationsOnly";
								if (isRelationsOnly)
								{

									if (picker.SavedValue == null)
									{
										picker.PickedKeys = new string[] { };
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
						}
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