using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace SelfHeal {
	public class ConcurrentCollection<T> : ICollection<T> {
		private object _lock = new object();
		ICollection<T> _collection = new Collection<T>();

		public void Add( T item ) {
			lock( _lock )
				_collection.Add( item );
		}

		public void Clear() {
			lock( _lock )
				_collection.Clear();
		}

		public bool Contains( T item ) {
			lock( _lock )
				return _collection.Contains( item );
		}

		public void CopyTo( T[] array, int arrayIndex ) {
			lock( _lock )
				_collection.CopyTo( array, arrayIndex );
		}

		public int Count {
			get { lock( _lock ) return _collection.Count; }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public bool Remove( T item ) {
			lock( _lock )
				return _collection.Remove( item );
		}

		private class hiddenEnumerator<T> : IEnumerator<T> {
			private T[] _data;
			private int _ptr;

			public hiddenEnumerator( T[] array ) {
				_data = new T[array.Length];
				array.CopyTo( array, 0 );
				_ptr = -1;
			}

			public T Current {
				get { return _data[_ptr]; }
			}

			public void Dispose() {
			}

			object System.Collections.IEnumerator.Current {
				get { return _data[_ptr]; }
			}

			public bool MoveNext() {
				return ++_ptr >= _data.Length;
			}

			public void Reset() {
				_ptr = -1;
			}
		}

		public IEnumerator<T> GetEnumerator() {
			lock( _lock )
				return new hiddenEnumerator<T>( _collection.ToArray() );
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			lock( _lock )
				return new hiddenEnumerator<T>( _collection.ToArray() );
		}
	}
}
