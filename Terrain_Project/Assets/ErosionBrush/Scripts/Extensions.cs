using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ErosionBrushPlugin
{
	static public class Extensions
	{
		public static bool InRange (this Rect rect, Vector2 pos) 
		{ 
			return (rect.center - pos).sqrMagnitude < (rect.width/2f)*(rect.width/2f); 
			//return rect.Contains(pos);
		}

		public static Vector3 V3 (this Vector2 v2) { return new Vector3(v2.x, 0, v2.y); }
		public static Vector2 V2 (this Vector3 v3) { return new Vector2(v3.x, v3.z); }
		public static Vector3 ToV3 (this float f) { return new Vector3(f,f, f); }

		public static Quaternion EulerToQuat (this Vector3 v) { Quaternion rotation = Quaternion.identity; rotation.eulerAngles = v; return rotation; }
		public static Quaternion EulerToQuat (this float f) { Quaternion rotation = Quaternion.identity; rotation.eulerAngles = new Vector3(0,f,0); return rotation; }

		public static Coord ToCoord (this Vector3 pos, float cellSize, bool ceil=false) //to use in object grid
		{
			if (!ceil) return new Coord(
				Mathf.FloorToInt((pos.x) / cellSize),
				Mathf.FloorToInt((pos.z) / cellSize) ); 
			else return new Coord(
				Mathf.CeilToInt((pos.x) / cellSize),
				Mathf.CeilToInt((pos.z) / cellSize) ); 
		}

		public static Coord ToCoord (this Vector2 pos) //to use in spatial hash (when sphash and matrix sizes are equal)
		{
			int posX = (int)(pos.x + 0.5f); if (pos.x < 0) posX--;
			int posZ = (int)(pos.y + 0.5f); if (pos.y < 0) posZ--;
			return new Coord(posX, posZ);
		}

		public static CoordRect ToCoordRect (this Vector3 pos, float range, float cellSize)
		{
			Coord size = (Vector3.one*range*2).ToCoord(cellSize, ceil:true) + 1;
			Coord offset = pos.ToCoord(cellSize) - size/2;
			return new CoordRect (offset, size);
		}

		public static List<Type> GetAllChildTypes (this Type type)
		{
			List<Type> result = new List<Type>();
		
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly(type);
			Type[] allTypes = assembly.GetTypes();
			for (int i=0; i<allTypes.Length; i++) 
				if (allTypes[i].IsSubclassOf(type)) result.Add(allTypes[i]); //nb: IsAssignableFrom will return derived classes

			return result;
		}

		public static Texture2D ColorTexture (int width, int height, Color color)
		{
			Texture2D result = new Texture2D(width, height);
			Color[] pixels = result.GetPixels(0,0,width,height);
			for (int i=0;i<pixels.Length;i++) pixels[i] = color;
			result.SetPixels(0,0,width,height, pixels);
			result.Apply();
			return result;
		}

		public static bool Equal (Vector3 v1, Vector3 v2)
		{
			return Mathf.Approximately(v1.x, v2.x) && 
					Mathf.Approximately(v1.y, v2.y) && 
					Mathf.Approximately(v1.z, v2.z);
		}
		
		public static bool Equal (Ray r1, Ray r2)
		{
			return Equal(r1.origin, r2.origin) && Equal(r1.direction, r2.direction);
		}

		public static void RemoveChildren (this Transform tfm)
		{
			for (int i=tfm.childCount-1; i>=0; i--)
			{
				Transform child = tfm.GetChild(i);
				GameObject.DestroyImmediate(child.gameObject); 
			}
		}

		public static void ToggleDisplayWireframe (this Transform tfm, bool show)
		{
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetSelectedWireframeHidden(tfm.GetComponent<Renderer>(), !show);
			int childCount = tfm.childCount;
			for (int c=0; c<childCount; c++) tfm.GetChild(c).ToggleDisplayWireframe(show);
			#endif
		}

		public static int ToInt (this Coord coord)
		{
			int absX = coord.x<0? -coord.x : coord.x; 
			int absZ = coord.z<0? -coord.z : coord.z;

			return ((coord.z<0? 1000000000 : 0) + absX*30000 + absZ) * (coord.x<0? -1 : 1);
		}

		public static Coord ToCoord (this int hash)
		{
			int absHash = hash<0? -hash : hash;
			int sign = (absHash/1000000000)*1000000000;

			int absX = (absHash - sign)/30000;
			int absZ = absHash - sign - absX*30000;

			return new Coord(hash<0? -absX : absX, sign==0? absZ : -absZ);
		}

		public static void CheckAdd<TKey,TValue> (this Dictionary<TKey,TValue> dict, TKey key, TValue value, bool replace=true)
		{
			if (dict.ContainsKey(key)) 
				{ if (replace) dict[key] = value; }
			else dict.Add(key, value);
		}
		public static void CheckRemove<TKey,TValue> (this Dictionary<TKey,TValue> dict, TKey key) { if (dict.ContainsKey(key)) dict.Remove(key); }
		public static TValue CheckGet<TKey,TValue> (this Dictionary<TKey,TValue> dict, TKey key)
		{
			if (dict.ContainsKey(key)) return dict[key];
			else return default(TValue);
		}

		public static void CheckAdd<T> (this HashSet<T> set, T obj) { if (!set.Contains(obj)) set.Add(obj); }
		public static void CheckRemove<T> (this HashSet<T> set, T obj) { if (set.Contains(obj)) set.Remove(obj); }
		public static void SetState<T> (this HashSet<T> set, T obj, bool state)
		{
			if (state && !set.Contains(obj)) set.Add(obj);
			if (!state && set.Contains(obj)) set.Remove(obj);
		}

		public static void Normalize (this float[,,] array, int pinnedLayer)
		{
			int maxX = array.GetLength(0); int maxZ = array.GetLength(1); int numLayers = array.GetLength(2);
			for (int x=0; x<maxX; x++)
				for (int z=0; z<maxZ; z++)
			{
				float othersSum = 0;

				for (int i=0; i<numLayers; i++)
				{
					if (i==pinnedLayer) continue;
					othersSum += array[x,z,i];
				}

				float pinnedValue = array[x,z,pinnedLayer];
				if (pinnedValue > 1) { pinnedValue = 1; array[x,z,pinnedLayer] = 1; }
				if (pinnedValue < 0) { pinnedValue = 0; array[x,z,pinnedLayer] = 0; }

				float othersTargetSum = 1 - pinnedValue;
				float factor = othersSum>0? othersTargetSum / othersSum : 0;

				for (int i=0; i<numLayers; i++)
				{
					if (i==pinnedLayer) continue;
					 array[x,z,i] *= factor;
				}
			}

		}

		#region Array

			public static int Find(this Array array, object obj)
			{
				for (int i=0; i<array.Length; i++)
					if (array.GetValue(i) == obj) return i;
				return -1;
			}

			static public int ArrayFind<T> (T[] array, T obj) where T : class
			{
				for (int i=0; i<array.Length; i++)
					if (array[i] == obj) return i;
				return -1;
			}
			

			static public void ArrayRemoveAt<T> (ref T[] array, int num) { array = ArrayRemoveAt(array, num); }
			static public T[] ArrayRemoveAt<T> (T[] array, int num)
			{
				T[] newArray = new T[array.Length-1];
				for (int i=0; i<newArray.Length; i++) 
				{
					if (i<num) newArray[i] = array[i];
					else newArray[i] = array[i+1];
				}
				return newArray;
			}

			static public void ArrayRemove<T> (ref T[] array, T obj) where T : class {array = ArrayRemove(array, obj); }
			static public T[] ArrayRemove<T> (T[] array, T obj) where T : class
			{
				int num = ArrayFind<T>(array, obj);
				return ArrayRemoveAt<T>(array,num);
			}
			

			static public void ArrayAdd<T> (ref T[] array, int after, T element=default(T)) { array = ArrayAdd(array, after, element:element); }
			static public T[] ArrayAdd<T> (T[] array, int after, T element=default(T))
			{
				if (array==null || array.Length==0) { return new T[] {element}; }
				if (after > array.Length-1) after = array.Length-1;
				
				T[] newArray = new T[array.Length+1];
				for (int i=0; i<newArray.Length; i++) 
				{
					if (i<=after) newArray[i] = array[i];
					else if (i == after+1) newArray[i] = element;
					else newArray[i] = array[i-1];
				}
				return newArray;
			}
			static public T[] ArrayAdd<T> (T[] array, T element=default(T)) { return ArrayAdd(array, array.Length-1, element); }
			static public void ArrayAdd<T> (ref T[] array, T element=default(T)) { array = ArrayAdd(array, array.Length-1, element); }

			static public void ArrayResize<T> (ref T[] array, int newSize, T element=default(T)) { array = ArrayResize(array, newSize, element); }
			static public T[] ArrayResize<T> (T[] array, int newSize, T element=default(T))
			{
				//NOTE: element is not unique. On adding 2 items it will fill both with one instance
				if (array.Length == newSize) return array;
				else if (newSize > array.Length) 
				{ 
					array = ArrayAdd(array, element); 
					array = ArrayResize(array,newSize); 
					return array; 
					}
				else 
				{ 
					array = ArrayRemoveAt(array, array.Length-1); 
					array = ArrayResize(array,newSize); 
					return array;
				}
			}


			static public void ArraySwitch<T> (T[] array, int num1, int num2)
			{
				if (num1<0 || num1>=array.Length || num2<0 || num2 >=array.Length) return;
				
				T temp = array[num1];
				array[num1] = array[num2];
				array[num2] = temp;
			}

			static public void ArraySwitch<T> (T[] array, T obj1, T obj2) where T : class
			{
				int num1 = ArrayFind<T>(array, obj1);
				int num2 = ArrayFind<T>(array, obj2);
				ArraySwitch<T>(array, num1, num2);
			}
		#endregion

		#region Array Sorting

			static public void ArrayQSort (float[] array) { ArrayQSort(array, 0, array.Length-1); }
			static public void ArrayQSort (float[] array, int l, int r)
			{
				float mid = array[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (array[i] < mid) i++;
					while (array[j] > mid) j--;
					if (i <= j)
					{
						float temp = array[i];
						array[i] = array[j];
						array[j] = temp;
						
						i++; j--;
					}
				}
				if (i < r) ArrayQSort(array, i, r);
				if (l < j) ArrayQSort(array, l, j);
			}

			static public void ArrayQSort<T> (T[] array, float[] reference) { ArrayQSort(array, reference, 0, reference.Length-1); }
			static public void ArrayQSort<T> (T[] array, float[] reference, int l, int r)
			{
				float mid = reference[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (reference[i] < mid) i++;
					while (reference[j] > mid) j--;
					if (i <= j)
					{
						float temp = reference[i];
						reference[i] = reference[j];
						reference[j] = temp;

						T tempT = array[i];
						array[i] = array[j];
						array[j] = tempT;
						
						i++; j--;
					}
				}
				if (i < r) ArrayQSort(array, reference, i, r);
				if (l < j) ArrayQSort(array, reference, l, j);
			}

			static public void ArrayQSort<T> (List<T> list, float[] reference) { ArrayQSort(list, reference, 0, reference.Length-1); }
			static public void ArrayQSort<T> (List<T> list, float[] reference, int l, int r)
			{
				float mid = reference[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (reference[i] < mid) i++;
					while (reference[j] > mid) j--;
					if (i <= j)
					{
						float temp = reference[i];
						reference[i] = reference[j];
						reference[j] = temp;

						T tempT = list[i];
						list[i] = list[j];
						list[j] = tempT;
						
						i++; j--;
					}
				}
				if (i < r) ArrayQSort(list, reference, i, r);
				if (l < j) ArrayQSort(list, reference, l, j);
			}

			static public int[] ArrayOrder (int[] array, int[] order=null, int max=0, int steps=1000000, int[] stepsArray=null) //returns an order int array
			{
				if (max==0) max=array.Length;
				if (stepsArray==null) stepsArray = new int[steps+1];
				else steps = stepsArray.Length-1;
			
				//creating starts array
				int[] starts = new int[steps+1];
				for (int i=0; i<max; i++) starts[ array[i] ]++;
					
				//making starts absolute
				int prev = 0;
				for (int i=0; i<starts.Length; i++)
					{ starts[i] += prev; prev = starts[i]; }

				//shifting starts
				for (int i=starts.Length-1; i>0; i--)
					{ starts[i] = starts[i-1]; }  
				starts[0] = 0;

				//using magic to compile order
				if (order==null) order = new int[max];
				for (int i=0; i<max; i++)
				{
					int h = array[i]; //aka height
					int num = starts[h];
					order[num] = i;
					starts[h]++;
				}
				return order;
			}

			static public T[] ArrayConvert<T,Y> (Y[] src)
			{
				T[] result = new T[src.Length];
				for (int i=0; i<src.Length; i++) result[i] = (T)(object)(src[i]);
				return result;
			}

		#endregion

	}//extensions
}//namespace
