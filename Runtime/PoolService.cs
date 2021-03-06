using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services
{
	/// <summary>
	/// This service allows to manage multiple pools of different types.
	/// The service can only a single pool of the same type. 
	/// </summary>
	public interface IPoolService
	{
		/// <summary>
		/// Adds the given <paramref name="pool"/> of <typeparamref name="T"/> to the service
		/// </summary>
		/// <exception cref="ArgumentException">
		/// Thrown if the service already has a pool of the given <typeparamref name="T"/> type
		/// </exception>
		void AddPool<T>(IObjectPool<T> pool);

		/// <summary>
		/// Removes the pool of the given <typeparamref name="T"/>
		/// </summary>
		void RemovePool<T>();

		/// <summary>
		/// Checks if exists a pool of the given type already exists or needs to be added before calling <seealso cref="Spawn{T}"/>
		/// </summary>
		bool HasPool<T>();

		/// <inheritdoc cref="HasPool{T}"/>
		bool HasPool(Type type);
		
		/// <inheritdoc cref="IObjectPool{T}.Spawn"/>
		/// <exception cref="ArgumentException">
		/// Thrown if the service does not contains a pool of the given <typeparamref name="T"/> type
		/// </exception>
		T Spawn<T>();
		
		/// <inheritdoc cref="IObjectPool{T}.Despawn"/>
		/// <exception cref="ArgumentException">
		/// Thrown if the service does not contains a pool of the given <typeparamref name="T"/> type
		/// </exception>
		void Despawn<T>(T entity);

		/// <inheritdoc cref="IObjectPool{T}.DespawnAll"/>
		/// <exception cref="ArgumentException">
		/// Thrown if the service does not contains a pool of the given <typeparamref name="T"/> type
		/// </exception>
		void DespawnAll<T>();
	}
	
	/// <inheritdoc />
	public class PoolService : IPoolService
	{
		private readonly Dictionary<Type, IObjectPool> _pools = new Dictionary<Type, IObjectPool>();

		/// <inheritdoc />
		public void AddPool<T>(IObjectPool<T> pool)
		{
			_pools.Add(typeof(T), pool);
		}

		/// <inheritdoc />
		public void RemovePool<T>()
		{
			_pools.Remove(typeof(T));
		}

		/// <inheritdoc />
		public bool HasPool<T>()
		{
			return HasPool(typeof(T));
		}

		/// <inheritdoc />
		public bool HasPool(Type type)
		{
			return _pools.ContainsKey(type);
		}

		/// <inheritdoc />
		public T Spawn<T>()
		{
			return GetPool<T>().Spawn();
		}

		/// <inheritdoc />
		public void Despawn<T>(T entity)
		{
			GetPool<T>().Despawn(entity);
		}

		/// <inheritdoc />
		public void DespawnAll<T>()
		{
			GetPool<T>().DespawnAll();
		}

		private IObjectPool<T> GetPool<T>()
		{
			if (!_pools.TryGetValue(typeof(T), out IObjectPool pool))
			{
				throw new ArgumentException("The pool was not initialized for the type " + typeof(T));
			}

			return pool as IObjectPool<T>;
		}
	}
	
	/// <summary>
	/// This interface allows pooled objects to be notified when it is spawned
	/// </summary>
	public interface IPoolEntitySpawn
	{
		/// <summary>
		/// Invoked when the Entity is spawned
		/// </summary>
		void OnSpawn();
	}
	
	/// <summary>
	/// This interface allows pooled objects to be notified when it is despawned
	/// </summary>
	public interface IPoolEntityDespawn
	{
		/// <summary>
		/// Invoked when the entity is despawned
		/// </summary>
		void OnDespawn();
	}

	/// <summary>
	/// Simple object pool implementation that can handle any type of entity objects
	/// </summary>
	public interface IObjectPool
	{
		/// <summary>
		/// Despawns all active spawned entities and returns them back to the pool to be used again later
		/// This function does not reset the entity. For that, have the entity implement <see cref="IPoolEntityDespawn"/> or do it externally
		/// </summary>
		void DespawnAll();
	}
	
	/// <inheritdoc />
	public interface IObjectPool<T> : IObjectPool
	{
		/// <summary>
		/// Spawns and returns an entity of the given type <typeparamref name="T"/>
		/// This function does not initialize the entity. For that, have the entity implement <see cref="IPoolEntitySpawn"/> or do it externally
		/// This function throws a <exception cref="StackOverflowException" /> if the pool is empty
		/// </summary>
		T Spawn();
		
		/// <summary>
		/// Despawns the given <paramref name="entity"/> and returns it back to the pool to be used again later
		/// This function does not reset the entity. For that, have the entity implement <see cref="IPoolEntityDespawn"/> or do it externally
		/// </summary>
		void Despawn(T entity);
	}

	/// <inheritdoc />
	public abstract class ObjectPoolBase<T> : IObjectPool<T>
	{
		private readonly Stack<T> _stack = new Stack<T>();
		private readonly IList<T> _spawnedEntities = new List<T>();
		private readonly Func<T, T> _instantiator;
		private readonly T _sampleEntity;
		
		protected ObjectPoolBase(int initSize, T sampleEntity, Func<T, T> instantiator)
		{
			_sampleEntity = sampleEntity;
			_instantiator = instantiator;
			
			for (var i = 0; i < initSize; i++)
			{
				_stack.Push(instantiator.Invoke(sampleEntity));
			}
		}

		/// <inheritdoc />
		public T Spawn()
		{
			var entity = _stack.Count == 0 ? _instantiator.Invoke(_sampleEntity) : _stack.Pop();
			var poolEntity = entity as IPoolEntitySpawn;
			
			_spawnedEntities.Add(entity);
			poolEntity?.OnSpawn();

			return entity;
		}

		/// <inheritdoc />
		public void Despawn(T entity)
		{
			var poolEntity = entity as IPoolEntityDespawn;

			_stack.Push(entity);
			_spawnedEntities.Remove(entity);
			poolEntity?.OnDespawn();
		}

		/// <inheritdoc />
		public void DespawnAll()
		{
			var entitiesCopy = new List<T>(_spawnedEntities);
			foreach (var entity in entitiesCopy)
			{
				Despawn(entity);
			}

			_spawnedEntities.Clear();
		}
	}

	/// <inheritdoc />
	public class ObjectPool<T> : ObjectPoolBase<T>
	{
		public ObjectPool(int initSize, Func<T> instantiator) : base(initSize, instantiator(), entityRef => instantiator.Invoke())
		{
		}
	}/// <inheritdoc />
	/// <remarks>
	/// Useful to for pools that use object references to create new instances (ex: GameObjects)
	/// </remarks>
	public class ObjectRefPool<T> : ObjectPoolBase<T>
	{
		public ObjectRefPool(int initSize, T sampleEntity, Func<T, T> instantiator) : base(initSize, sampleEntity, instantiator)
		{
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// Useful to for pools that use object references to create new <see cref="GameObject"/>
	/// </remarks>
	public class GameObjectPool<T> : ObjectRefPool<T> where T : MonoBehaviour
	{
		public GameObjectPool(int initSize, T sampleEntity) : base(initSize, sampleEntity, Instantiator)
		{
		}

		/// <summary>
		/// Generic instantiator for <see cref="GameObject"/> pools
		/// </summary>
		/// <param name="entityRef"></param>
		/// <returns></returns>
		public static T Instantiator(T entityRef)
		{
			var instance = Object.Instantiate(entityRef, entityRef.transform.parent, true);

			instance.gameObject.SetActive(false);

			return instance;
		}
	}
}