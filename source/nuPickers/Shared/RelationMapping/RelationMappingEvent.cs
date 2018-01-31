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
		
		// Picker Lists grouped by each entity being saved
		private Dictionary<Guid, List<Picker>> allSavedPickers;

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

			allSavedPickers = new Dictionary<Guid, List<Picker>>();

			foreach (IContent entity in changedEntities)
			{

				if (entity != null)
				{

					List<Picker> entityPickers = new List<Picker>();

					// Loop all Pickers in saved Content item
					foreach (PropertyType propertyType in entity.PropertyTypes.Where(x => PickerPropertyValueConverter.IsPicker(x.PropertyEditorAlias)))
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
														
							entityPickers.Add(picker); // Retain any relation-mapped Picker(s) for Save event													
						}
					}

					allSavedPickers.Add(entity.Key, entityPickers);
				}
			}
		}

		private void Saved(IService sender, IEnumerable<IContentBase> changedEntities)
		{
			foreach (IContent entity in changedEntities)
			{
				if (entity != null)
				{

					// Loop Picker editors for current saved entity

					Picker picker = null;

					foreach (PropertyType propertyType in entity.PropertyTypes.Where(x => PickerPropertyValueConverter.IsPicker(x.PropertyEditorAlias)))
					{
						try
						{
							picker = allSavedPickers[entity.Key].FirstOrDefault(x => x.PropertyAlias == propertyType.Alias);
						} catch(Exception ex)
						{
							// TODO Handle as seen fit
							Console.WriteLine(ex.Message);
						}						

						if (picker == null)
						{
							continue;
						}

						bool isRelationsOnly = picker.GetDataTypePreValue("saveFormat").Value == "relationsOnly";
												
						RelationMapping.UpdateRelationMapping(
							entity.Id,
							picker.PropertyAlias,
							picker.RelationTypeAlias,
							isRelationsOnly,
							picker.PickedIds.ToArray());
					}
				}
			}

			allSavedPickers = null;
		}
	}
}