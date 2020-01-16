﻿using Recipes.ModelWithChildren;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using LLBLGenPro.OrmCookbook.DatabaseSpecific;
using LLBLGenPro.OrmCookbook.EntityClasses;
using LLBLGenPro.OrmCookbook.HelperClasses;
using LLBLGenPro.OrmCookbook.Linq;
using SD.LLBLGen.Pro.LinqSupportClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;

namespace Recipes.LLBLGenPro.ModelWithChildren
{
	public class ModelWithChildrenScenario : IModelWithChildrenScenario<ProductLineEntity, ProductEntity>
	{
		public int Create(ProductLineEntity productLine)
		{
			if (productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			using (var adapter = new DataAccessAdapter())
			{
				adapter.SaveEntity(productLine);
				return productLine.ProductLineKey;
			}
		}

		public void Delete(ProductLineEntity productLine)
		{
			if (productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			// We'll have to delete the whole graph so first the related entities, then the main entity.
			// Let's use a Unit of work here for that.
			var uow = new UnitOfWork2();
			uow.AddCollectionForDelete(productLine.Products);
			uow.AddForDelete(productLine);
			using (var adapter = new DataAccessAdapter())
			{
				uow.Commit(adapter);
			}
		}

		public void DeleteByKey(int productLineKey)
		{
			// let's directly delete the entities, without fetching them. Use a unit of work for this 
			// to wrap everything neatly in a transaction when it's committed. A Unit of work is a 
			// persistence agnostic object you can pass on freely to add work and then have all the work 
			// performed in a single transaction.
			var uow = new UnitOfWork2();
			uow.AddDeleteEntitiesDirectlyCall(typeof(ProductEntity), new RelationPredicateBucket(ProductFields.ProductLineKey.Equal(productLineKey)));
			uow.AddDeleteEntitiesDirectlyCall(typeof(ProductLineEntity), new RelationPredicateBucket(ProductLineFields.ProductLineKey.Equal(productLineKey)));
			using (var adapter = new DataAccessAdapter())
			{
				uow.Commit(adapter);
			}
		}

		public IList<ProductLineEntity> FindByName(string productLineName, bool includeProducts)
		{
			using (var adapter = new DataAccessAdapter())
			{
				var metaData = new LinqMetaData(adapter);
				if(includeProducts)
				{
					return metaData.ProductLine.Where(x => x.ProductLineName == productLineName).WithPath(p => p.Prefetch(pl => pl.Products)).ToList();
				}
				return metaData.ProductLine.Where(x => x.ProductLineName == productLineName).ToList();
			}
		}

		public IList<ProductLineEntity> GetAll(bool includeProducts)
		{
			using (var adapter = new DataAccessAdapter())
			{
				var metaData = new LinqMetaData(adapter);
				if(includeProducts)
				{
					return metaData.ProductLine.WithPath(p => p.Prefetch(pl => pl.Products)).ToList();
				}
				return metaData.ProductLine.ToList();
			}
		}

		public ProductLineEntity? GetByKey(int productLineKey, bool includeProducts)
		{
			using (var adapter = new DataAccessAdapter())
			{
				var metaData = new LinqMetaData(adapter);
				if(includeProducts)
				{
					var toReturn = metaData.ProductLine.Where(x => x.ProductLineKey == productLineKey).WithPath(p => p.Prefetch(pl => pl.Products)).SingleOrDefault();
					if(toReturn != null)
					{
						// insert removal tracker for tracking removed entities. 
						toReturn.Products.RemovedEntitiesTracker = new EntityCollection<ProductEntity>();
					}
					return toReturn;
				}
				return metaData.ProductLine.SingleOrDefault(x => x.ProductLineKey == productLineKey);
			}
		}


		public virtual void Update(ProductLineEntity productLine)
		{
			if(productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			// this update method will update the related products. Any removed product has to be removed as it's orphaned. 
			// We have to remove all products which key isn't in the set of products currently related to the passed in productline.
			// To do that we'll do a delete directly using a where clause where all entities with a key not in the set of
			// keys of the current related product entities are removed. We'll wrap it all in a unit of work for easy transaction handling.
			// In the unit of work, we have to schedule the direct deletes before the insert of the new row, otherwise it's removed,
			// as it doesn't have a PK yet, so the IN clause we're using won't match it.
			var currentKeys = productLine.Products.Select(p => p.ProductKey).ToList();
			var uow = new UnitOfWork2(new List<UnitOfWorkBlockType>() {UnitOfWorkBlockType.DeletesPerformedDirectly, UnitOfWorkBlockType.Inserts, UnitOfWorkBlockType.Updates});
			uow.AddDeleteEntitiesDirectlyCall(typeof(ProductEntity), new RelationPredicateBucket(ProductFields.ProductKey.NotIn(currentKeys)));
			uow.AddForSave(productLine);
			using (var adapter = new DataAccessAdapter())
			{
				uow.Commit(adapter);
			}
		}

		public void Update(ProductEntity product)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product), $"{nameof(product)} is null.");

			using (var adapter = new DataAccessAdapter())
			{
				adapter.SaveEntity(product);
			}
		}
	}
}