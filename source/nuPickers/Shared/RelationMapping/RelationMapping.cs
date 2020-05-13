namespace nuPickers.Shared.RelationMapping
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Umbraco.Core;
	using Umbraco.Core.Models;

	/// <summary>
	/// the core relation mapping functionality
	/// </summary>
	internal class RelationMapping
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="contextId">the id of the content, media or member item</param>
		/// <param name="propertyAlias">the property alias of the picker using relation mapping</param>
		/// <param name="relationTypeAlias">the alias of the relation type to use</param>
		/// <param name="relationsOnly"></param>
		/// <returns></returns>
		internal static IEnumerable<int> GetRelatedIds(int contextId, string propertyAlias, string relationTypeAlias, bool relationsOnly)
		{
			IRelationType relationType = ApplicationContext.Current.Services.RelationService.GetRelationTypeByAlias(relationTypeAlias);

			if (relationType != null)
			{
				// get all relations of this type
				IEnumerable<IRelation> relations = FilterRelationsFromList(relationType, contextId, propertyAlias, relationsOnly);

				return relations.Select(x => (x.ParentId != contextId) ? x.ParentId : x.ChildId);
			}

			return null;
		}

		internal static IEnumerable<IRelation> FilterRelationsFromList(IRelationType relationType, int contextId, string propertyAlias, bool relationsOnly)
		{
			IEnumerable<IRelation> relations = ApplicationContext.Current.Services.RelationService.GetAllRelationsByRelationType(relationType.Id);

			// construct object used to identify a relation (this is serialized into the relation comment field)
			RelationMappingComment relationMappingComment = new RelationMappingComment(contextId, propertyAlias);

			// filter down potential relations, by relation type direction
			if (relationType.IsBidirectional && relationsOnly)
			{
				relations = relations.Where(x => x.ChildId == contextId || x.ParentId == contextId);
				relations = relations.Where(x => new RelationMappingComment(x.Comment).DataTypeDefinitionId == relationMappingComment.DataTypeDefinitionId);

				/* 
					For bi-directional Relations use the Sort Order related to the current contextId (node where the Relation was last saved). 
					This may be the parentSortOrder for some records and childSortOrder for others depending on which node the editor used most recently to save the Relation.
				*/
				return relations.OrderByDescending(x => x.ChildId == contextId ? new RelationMappingComment(x.Comment).ChildSortOrder : new RelationMappingComment(x.Comment).ParentSortOrder);
				
			}
			else
			{
				relations = relations.Where(x => x.ChildId == contextId);
				relations = relations.Where(x => new RelationMappingComment(x.Comment).PropertyTypeId == relationMappingComment.PropertyTypeId);

				if (relationMappingComment.IsInArchetype())
				{
					relations = relations.Where(x => new RelationMappingComment(x.Comment).MatchesArchetypeProperty(relationMappingComment.PropertyAlias));
				}

				return relations.OrderByDescending(x => new RelationMappingComment(x.Comment).ParentSortOrder);
			}						

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="contextId">the id of the content, media or member item</param>
		/// <param name="propertyAlias">the property alias of the picker using relation mapping</param>
		/// <param name="relationTypeAlias">the alias of the relation type to use</param>
		/// <param name="relationsOnly"></param>
		/// <param name="pickedIds">the ids of all picked items that are to be related to the contextId</param>
		internal static void UpdateRelationMapping(int contextId, string propertyAlias, string relationTypeAlias, bool relationsOnly, int[] pickedIds)
		{

			IRelationType relationType = ApplicationContext.Current.Services.RelationService.GetRelationTypeByAlias(relationTypeAlias);

			if (relationType != null)
			{
				// get all relations of this type
				List<IRelation> relations = FilterRelationsFromList(relationType, contextId, propertyAlias, relationsOnly).ToList();
				
				// check current context is of the correct object type (as according to the relation type)
				if (ApplicationContext.Current.Services.EntityService.GetObjectType(contextId) == UmbracoObjectTypesExtensions.GetUmbracoObjectType(relationType.ChildObjectType))
				{
					// we need a sort-order nr here.
					var currentSortOrder = pickedIds.Length;

					// for each picked item 
					foreach (int pickedId in pickedIds)
					{
						// check picked item context if of the correct object type (as according to the relation type)
						if (ApplicationContext.Current.Services.EntityService.GetObjectType(pickedId) == UmbracoObjectTypesExtensions.GetUmbracoObjectType(relationType.ParentObjectType))
						{
							// If relation doesn't already exist (new picked item) TODO S6 What about bi-directional? "Matching" record may exist but ids will be swapped.
							// Have those already been filtered out by FilterRelationsFromList above?
							// If old records are being deleted in favor of new ones each time the parent/child Ids are swapped, we're losing any previous value for ChildSortOrder
							if (!relations.Exists(x => x.ParentId == pickedId))
							{								
								// create relation
								Relation relation = new Relation(pickedId, contextId, relationType);
								var comment = new RelationMappingComment(contextId, propertyAlias);

								IRelation biRelation = relations.FirstOrDefault(x => x.ChildId == pickedId);
								if (biRelation != null)
								{
									var xml = new RelationMappingComment(biRelation.Comment);
									// Transfer any previous ChildSortOrder value from the opposite direction relation before it gets deleted and replaced with the new record below
									comment.ParentSortOrder = xml.ChildSortOrder; // Yes, Child needs to be swapped into Parent because the relationship is switching directions
								}

								comment.ChildSortOrder = currentSortOrder; // Counterintuitive, Child is actually the id of the node being edited, not Parent

								relation.Comment = comment.GetComment();
								ApplicationContext.Current.Services.RelationService.Save(relation);
							}
							else
							{
								// update sort order
								var relation = relations.First(x => x.ParentId == pickedId);
								var mapping = new RelationMappingComment(relation.Comment);
								//mapping.ParentSortOrder
								mapping.ChildSortOrder = currentSortOrder;								
								relation.Comment = mapping.GetComment();
								ApplicationContext.Current.Services.RelationService.Save(relation);
							}

							currentSortOrder--;

							// housekeeping - remove 'the' relation from the list being processed (there should be only one)
							relations.RemoveAll(x => x.ChildId == contextId && x.ParentId == pickedId && x.RelationTypeId == relationType.Id);
						} else
						{
							Console.WriteLine("Mismatched ObjectType for pickedId " + pickedId);
						}
					}
				}

				// delete relations for any items left on the list being processed
				if (relations.Any())
				{
					foreach (IRelation relation in relations)
					{						
						ApplicationContext.Current.Services.RelationService.Delete(relation);
					}
				}
			}
		}
	}
}