// 
//  TObject.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
//       Tobias Downer <toby@mckoi.com>
//  
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.Runtime.Serialization;

using Deveel.Math;

namespace Deveel.Data {
	/// <summary>
	/// A <see cref="TObject"/> is a strongly typed object in a database engine.
	/// </summary>
	/// <remarks>
	/// A <see cref="TObject"/> must maintain type information (eg. STRING, NUMBER, 
	/// etc) along with the object value being represented itself.
	/// </remarks>
	[Serializable]
	public sealed class TObject : IDeserializationCallback/*, IComparable*/ {
		/// <summary>
		/// The type of this object.
		/// </summary>
		private readonly TType type;

		/// <summary>
		/// The representation of the object.
		/// </summary>
		private object ob;

		public TObject(TType type, Object ob) {
			this.type = type;
			if (ob is String) {
				this.ob = StringObject.FromString((String)ob);
			} else {
				this.ob = ob;
			}
		}

		/// <summary>
		/// Returns the type of this object.
		/// </summary>
		public TType TType {
			get { return type; }
		}

		/// <summary>
		/// Returns true if the object is null.
		/// </summary>
		/// <remarks>
		/// Note that we must still be able to determine type information 
		/// for an object that is NULL.
		/// </remarks>
		public bool IsNull {
			get { return (Object == null); }
		}

		/// <summary>
		/// Returns a <see cref="System.Object"/> that is the data behind this object.
		/// </summary>
		public object Object {
			get { return ob; }
		}

		/// <summary>
		/// Returns the approximate memory use of this object in bytes.
		/// </summary>
		/// <remarks>
		/// This is used when the engine is caching objects and we need a 
		/// general indication of how much space it takes up in memory.
		/// </remarks>
		public int ApproximateMemoryUse {
			get { return TType.CalculateApproximateMemoryUse(Object); }
		}

		/// <summary>
		/// Returns true if the type of this object is logically comparable to the
		/// type of the given object.
		/// </summary>
		/// <param name="obj"></param>
		/// <remarks>
		/// For example, VARCHAR and LONGVARCHAR are comparable types.  DOUBLE and 
		/// FLOAT are comparable types.  DOUBLE and VARCHAR are not comparable types.
		/// </remarks>
		public bool ComparableTypes(TObject obj) {
			return TType.IsComparableType(obj.TType);
		}

		/// <summary>
		/// Gets the value of the object as a <see cref="BigNumber"/>.
		/// </summary>
		/// <returns>
		/// Returns a <see cref="BigNumber"/> if <see cref="TObject.TType"/> is a
		/// <see cref="TNumericType"/>, <b>null</b> otherwise.
		/// </returns>
		public BigNumber ToBigNumber() {
			if (TType is TNumericType)
				return (BigNumber)Object;
			return null;
		}

		/// <summary>
		/// Gets the value of the object as a <see cref="Number"/>.
		/// </summary>
		/// <param name="isNull"></param>
		/// <returns>
		/// Returns a <see cref="Boolean"/> if <see cref="TObject.TType"/> is a
		/// <see cref="TBooleanType"/>.
		/// </returns>
		/// <exception cref="InvalidCastException">
		/// If the value wrapped by this object is not a <see cref="TBooleanType"/>.
		/// </exception>
		public bool ToBoolean(out bool isNull) {
			if (TType is TBooleanType) {
				isNull = false;
				return (Boolean)Object;
			}
			isNull = true;
			return false;
		}

		/// <summary>
		/// Gets the value of the object as a <see cref="Number"/>.
		/// </summary>
		/// <returns>
		/// Returns a <see cref="Boolean"/> if <see cref="TObject.TType"/> is a
		/// <see cref="TBooleanType"/>.
		/// </returns>
		/// <exception cref="InvalidCastException">
		/// If the value wrapped by this object is not a <see cref="TBooleanType"/>.
		/// </exception>
		/// <seealso cref="ToBoolean(out bool)"/>
		public bool ToBoolean() {
			bool isNull;
			return ToBoolean(out isNull);
		}

		/// <summary>
		/// Returns the String of this object if this object is a string type.
		/// </summary>
		/// <remarks>
		/// If the object is not a string type or is NULL then a null object is
		/// returned.  This method must not be used to cast from a type to a string.
		/// </remarks>
		/// <returns></returns>
		public String ToStringValue() {
			if (TType is TStringType) {
				return Object.ToString();
			}
			return null;
		}


		public static readonly TObject BooleanTrue = new TObject(TType.BooleanType, true);
		public static readonly TObject BooleanFalse = new TObject(TType.BooleanType, false);
		public static readonly TObject BooleanNull = new TObject(TType.BooleanType, null);

		public static readonly TObject NullObject = new TObject(TType.NullType, null);

		/// <summary>
		/// Returns a TObject of boolean type that is either true or false.
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public static TObject GetBoolean(bool b) {
			return b ? BooleanTrue : BooleanFalse;
		}

		/// <summary>
		/// Returns a TObject of numeric type that represents the given int value.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public static TObject GetInt4(int val) {
			return GetBigNumber(BigNumber.fromLong(val));
		}

		/// <summary>
		/// Returns a TObject of numeric type that represents the given long value.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public static TObject GetInt8(long val) {
			return GetBigNumber(BigNumber.fromLong(val));
		}

		/// <summary>
		/// Returns a TObject of numeric type that represents the given double value.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public static TObject GetDouble(double val) {
			return GetBigNumber(BigNumber.fromDouble(val));
		}

		/// <summary>
		/// Returns a TObject of numeric type that represents the given BigNumber value.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public static TObject GetBigNumber(BigNumber val) {
			return new TObject(TType.NumericType, val);
		}

		/// <summary>
		/// Returns a TObject of VARCHAR type that represents the given 
		/// <see cref="StringObject"/> value.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static TObject GetString(StringObject str) {
			return new TObject(TType.StringType, str);
		}

		/// <summary>
		/// Returns a TObject of VARCHAR type that represents the given 
		/// <see cref="string"/> value.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static TObject GetString(String str) {
			return new TObject(TType.StringType, StringObject.FromString(str));
		}

		/// <summary>
		/// Returns a TObject of DATE type that represents the given time value.
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public static TObject GetDateTime(DateTime d) {
			return new TObject(TType.DateType, d);
		}

		/// <summary>
		/// Returns a TObject of NULL type that represents a null value.
		/// </summary>
		public static TObject Null {
			get { return NullObject; }
		}

		/// <summary>
		/// Returns a TObject from the given value.
		/// </summary>
		/// <param name="ob"></param>
		/// <returns></returns>
		public static TObject GetObject(Object ob) {
			if (ob == null)
				return Null;
			if (ob is BigNumber)
				return GetBigNumber((BigNumber)ob);
			if (ob is StringObject)
				return GetString((StringObject)ob);
			if (ob is Boolean)
				return GetBoolean((Boolean)ob);
			if (ob is DateTime)
				return GetDateTime((DateTime)ob);
			if (ob is ByteLongObject)
				return new TObject(TType.BinaryType, (ByteLongObject)ob);
			if (ob is byte[])
				return new TObject(TType.BinaryType, new ByteLongObject((byte[])ob));
			if (ob is IBlobRef)
				return new TObject(TType.BinaryType, (IBlobRef)ob);
			if (ob is IClobRef)
				return new TObject(TType.StringType, (IClobRef)ob);
			
			throw new ArgumentException("Don't know how to convert object type " + ob.GetType());
		}

		/// <summary>
		/// Compares this object with the given object (which is of a logically
		/// comparable type).
		/// </summary>
		/// <remarks>
		/// This cannot be used to compare null values so it assumes that checks
		/// for null have already been made.
		/// </remarks>
		/// <returns>
		/// Returns 0 if the value of the objects are equal, -1 if this object is smaller 
		/// than the given object, and 1 if this object is greater than the given object.
		/// </returns>
		public int CompareToNoNulls(TObject tob) {
			TType ttype = TType;
			// Strings must be handled as a special case.
			if (ttype is TStringType) {
				// We must determine the locale to compare against and use that.
				TStringType stype = (TStringType)type;
				// If there is no locale defined for this type we use the locale in the
				// given type.
				if (stype.Locale == null) {
					ttype = tob.TType;
				}
			}
			return ttype.Compare(Object, tob.Object);
		}


		/// <summary>
		/// Compares this object with the given object (which is of a logically
		/// comparable type).
		/// </summary>
		/// <remarks>
		/// This compares <c>NULL</c> values before non null values, and null values are
		/// equal.
		/// </remarks>
		/// <returns>
		/// Returns 0 if the value of the objects are equal, -1 if this object is smaller 
		/// than the given object, 1 if this object is greater than the given object.
		/// </returns>
		/// <seealso cref="CompareToNoNulls"/>
		public int CompareTo(TObject tob) {
			// If this is null
			if (IsNull) {
				// and value is null return 0 return less
				if (tob.IsNull)
					return 0;
				return -1;
			}
			// If this is not null and value is null return +1
			if (tob.IsNull)
				return 1;
			// otherwise both are non null so compare normally.
			return CompareToNoNulls(tob);
		}

		/*
		int IComparable.CompareTo(object obj) {
			if (!(obj is TObject))
				throw new ArgumentException();
			return CompareTo((TObject)obj);
		}
		*/

		/// <inheritdoc/>
		/// <exception cref="ApplicationException">
		/// It's not clear what we would be testing the equality of with this method.
		/// </exception>
		public override bool Equals(object obj) {
			throw new ApplicationException("equals method should not be used.");
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return base.GetHashCode();
		}

		/**
		 * Equality test.  Returns true if this object is equivalent to the given
		 * TObject.  This means the types are the same, and the object itself is the
		 * same.
		 */
		public bool ValuesEqual(TObject obj) {
			if (this == obj) {
				return true;
			}
			if (TType.IsComparableType(obj.TType)) {
				return CompareTo(obj) == 0;
			}
			return false;
		}



		// ---------- Object operators ----------

		#region Operators

		/*
		public static TObject operator |(TObject a, TObject b) {
			return a.Or(b);
		}

		public static TObject operator +(TObject a, TObject b) {
			return a.Add(b);
		}

		public static TObject operator -(TObject a, TObject b) {
			return a.Subtract(b);
		}

		public static TObject operator *(TObject a, TObject b) {
			return a.Multiply(b);
		}

		public static TObject operator /(TObject a, TObject b) {
			return a.Divide(b);
		}

		public static TObject operator >(TObject a, TObject b) {
			return a.Greater(b);
		}

		public static TObject operator >=(TObject a, TObject b) {
			return a.GreaterEquals(b);
		}

		public static TObject operator <(TObject a, TObject b) {
			return a.Less(b);
		}

		public static TObject operator <=(TObject a, TObject b) {
			return a.LessEquals(b);
		}

		public static TObject operator !(TObject a) {
			return a.Not();
		}
		*/

		#endregion

		/// <summary>
		/// Bitwise <b>OR</b> operation of this object with the given object.
		/// </summary>
		/// <param name="val">The operand object of the operation.</param>
		/// <returns>
		/// If <see cref="Type"/> is a <see cref="TNumericType"/>, it returns the result 
		/// of the bitwise-or between this object value and the given one. If either numeric 
		/// value has a scale of 1 or greater then it returns <b>null</b>. If this or the given 
		/// object is not a numeric type then it returns <b>null</b>. If either this object or 
		/// the given object is <c>NULL</c>, then the <c>NULL</c> object is returned.
		/// </returns>
		public TObject Or(TObject val) {
			BigNumber v1 = ToBigNumber();
			BigNumber v2 = val.ToBigNumber();
			TType result_type = TType.GetWidestType(TType, val.TType);

			if (v1 == null || v2 == null) {
				return new TObject(result_type, null);
			}

			return new TObject(result_type, v1.BitWiseOr(v2));
		}

		/// <summary>
		/// Performs an addition between the value of this object and that of the specified.
		/// </summary>
		/// <returns>
		/// If this or the given object is not a numeric or interval type then it 
		/// returns null. If either this object or the given object is <c>NULL</c>, 
		/// then the <c>NULL</c> object is returned. Returns the mathematical addition
		/// between this numeric value and the given one if <see cref="TObject.TType"/>
		/// is <see cref="TNumericType"/>.
		/// </returns>
		public TObject Add(TObject val) {
			BigNumber v1 = ToBigNumber();
			BigNumber v2 = val.ToBigNumber();
			TType result_type = TType.GetWidestType(TType, val.TType);

			if (v1 == null || v2 == null) {
				return new TObject(result_type, null);
			}

			return new TObject(result_type, v1.Add(v2));
		}

		/// <summary>
		/// Performs a subtraction of the value of the current object to the given one.
		/// </summary>
		/// <returns>
		/// If this or the given object is not a numeric or interval type then it returns null.
		/// If either this object or the given object is <c>NULL</c>, then the <c>NULL</c> 
		/// object is returned.
		/// </returns>
		public TObject Subtract(TObject val) {
			BigNumber v1 = ToBigNumber();
			BigNumber v2 = val.ToBigNumber();
			TType result_type = TType.GetWidestType(TType, val.TType);

			if (v1 == null || v2 == null) {
				return new TObject(result_type, null);
			}

			return new TObject(result_type, v1.Subtract(v2));
		}

		/// <summary>
		/// Mathematical multiply of this object to the given object.
		/// </summary>
		/// <returns>
		/// If this or the given object is not a numeric type then it returns null.
		/// If either this object or the given object is NULL, then the NULL object
		/// is returned.
		/// </returns>
		public TObject Multiply(TObject val) {
			BigNumber v1 = ToBigNumber();
			BigNumber v2 = val.ToBigNumber();
			TType result_type = TType.GetWidestType(TType, val.TType);

			if (v1 == null || v2 == null) {
				return new TObject(result_type, null);
			}

			return new TObject(result_type, v1.Multiply(v2));
		}

		/// <summary>
		/// Mathematical division of this object to the given object.
		/// </summary>
		/// <returns>
		/// If this or the given object is not a numeric type then it returns null.
		/// If either this object or the given object is NULL, then the NULL object
		/// is returned.
		/// </returns>
		public TObject Divide(TObject val) {
			BigNumber v1 = ToBigNumber();
			BigNumber v2 = val.ToBigNumber();
			TType result_type = TType.GetWidestType(TType, val.TType);

			if (v1 == null || v2 == null) {
				return new TObject(result_type, null);
			}

			return new TObject(result_type, v1.Divide(v2));
		}

		/// <summary>
		/// String concat of this object to the given object.
		/// </summary>
		/// <returns>
		/// If this or the given object is not a string type then it returns null.  
		/// If either this object or the given object is NULL, then the NULL object 
		/// is returned.
		/// </returns>
		/// <remarks>
		/// This operator always returns an object that is a VARCHAR string type of
		/// unlimited size with locale inherited from either this or val depending
		/// on whether the locale information is defined or not.
		/// </remarks>
		public TObject Concat(TObject val) {
			// If this or val is null then return the null value
			if (IsNull)
				return this;
			if (val.IsNull)
				return val;

			TType tt1 = TType;
			TType tt2 = val.TType;

			if (tt1 is TStringType &&
				tt2 is TStringType) {
				// Pick the first locale,
				TStringType st1 = (TStringType)tt1;
				TStringType st2 = (TStringType)tt2;

				CultureInfo str_locale = null;
				Text.CollationStrength str_strength = 0;
				Text.CollationDecomposition str_decomposition = 0;

				if (st1.Locale != null) {
					str_locale = st1.Locale;
					str_strength = st1.Strength;
					str_decomposition = st1.Decomposition;
				} else if (st2.Locale != null) {
					str_locale = st2.Locale;
					str_strength = st2.Strength;
					str_decomposition = st2.Decomposition;
				}

				TStringType dest_type = st1;
				if (str_locale != null) {
					dest_type = new TStringType(SQLTypes.VARCHAR, -1,
											 str_locale, str_strength, str_decomposition);
				}

				return new TObject(dest_type,
						StringObject.FromString(ToStringValue() + val.ToStringValue()));

			}

			// Return null if LHS or RHS are not strings
			return new TObject(tt1, null);
		}

		/// <summary>
		/// Comparison of this object and the given object.
		/// </summary>
		/// <remarks>
		/// The compared objects must be the same type otherwise it returns false. 
		/// This is able to compare null values.
		/// </remarks>
		public TObject Is(TObject val) {
			if (IsNull && val.IsNull)
				return BooleanTrue;
			if (ComparableTypes(val))
				return GetBoolean(CompareTo(val) == 0);
			// Not comparable types so return false
			return BooleanFalse;
		}

		/**
		 * Comparison of this object and the given object.  The compared objects
		 * must be the same type otherwise it returns null (doesn't know).  If either
		 * this object or the given object is NULL then NULL is returned.
		 */
		public TObject IsEqual(TObject val) {
			// Check the types are comparable
			if (ComparableTypes(val) && !IsNull && !val.IsNull) {
				return GetBoolean(CompareToNoNulls(val) == 0);
			}
			// Not comparable types so return null
			return BooleanNull;
		}

		/**
		 * Comparison of this object and the given object.  The compared objects
		 * must be the same type otherwise it returns null (doesn't know).  If either
		 * this object or the given object is NULL then NULL is returned.
		 */
		public TObject IsNotEqual(TObject val) {
			// Check the types are comparable
			if (ComparableTypes(val) && !IsNull && !val.IsNull) {
				return GetBoolean(CompareToNoNulls(val) != 0);
			}
			// Not comparable types so return null
			return BooleanNull;
		}

		/// <summary>
		/// Comparison of this object and the given object.
		/// </summary>
		/// <remarks>
		/// The compared objects must be the same type otherwise it returns 
		/// null (doesn't know).
		/// </remarks>
		/// <returns>
		/// If either this object or the given object is NULL then NULL is 
		/// returned.
		/// </returns>
		public TObject Greater(TObject val) {
			// Check the types are comparable
			if (ComparableTypes(val) && !IsNull && !val.IsNull) {
				return GetBoolean(CompareToNoNulls(val) > 0);
			}
			// Not comparable types so return null
			return BooleanNull;
		}

		/// <summary>
		/// Comparison of this object and the given object.
		/// </summary>
		/// <remarks>
		/// The compared objects must be the same type otherwise it returns 
		/// null (doesn't know).
		/// </remarks>
		/// <returns>
		/// If either this object or the given object is NULL then NULL is 
		/// returned.
		/// </returns>
		public TObject GreaterEquals(TObject val) {
			// Check the types are comparable
			if (ComparableTypes(val) && !IsNull && !val.IsNull) {
				return GetBoolean(CompareToNoNulls(val) >= 0);
			}
			// Not comparable types so return null
			return BooleanNull;
		}

		/// <summary>
		/// Comparison of this object and the given object.
		/// </summary>
		/// <remarks>
		/// The compared objects must be the same type otherwise it returns 
		/// null (doesn't know).</remarks>
		/// <returns>
		/// If either this object or the given object is NULL then NULL is 
		/// returned.
		/// </returns>
		public TObject Less(TObject val) {
			// Check the types are comparable
			if (ComparableTypes(val) && !IsNull && !val.IsNull) {
				return GetBoolean(CompareToNoNulls(val) < 0);
			}
			// Not comparable types so return null
			return BooleanNull;
		}

		/// <summary>
		/// Comparison of this object and the given object.
		/// </summary>
		/// <remarks>
		/// The compared objects must be the same type otherwise it 
		/// returns null (doesn't know).</remarks>
		/// <returns>
		/// If either this object or the given object is NULL then NULL is 
		/// returned.
		/// </returns>
		public TObject LessEquals(TObject val) {
			// Check the types are comparable
			if (ComparableTypes(val) && !IsNull && !val.IsNull) {
				return GetBoolean(CompareToNoNulls(val) <= 0);
			}
			// Not comparable types so return null
			return BooleanNull;
		}


		/// <summary>
		/// Performs a logical NOT on this value.
		/// </summary>
		/// <returns>
		/// </returns>
		public TObject Not() {
			// If type is null
			if (IsNull) {
				return this;
			}
			bool isNull;
			bool b = ToBoolean(out isNull);
			if (!isNull)
				return GetBoolean(!b);
			return BooleanNull;
		}




		// ---------- Casting methods -----------

		/**
		 * Returns a TObject of the given type and with the given object.  If
		 * the object is not of the right type then it is cast to the correct type.
		 */
		public static TObject CreateAndCastFromObject(TType type, Object ob) {
			return new TObject(type, TType.CastObjectToTType(ob, type));
		}

		/// <summary>
		/// Casts this object to the given type and returns a new <see cref="TObject"/>.
		/// </summary>
		/// <param name="cast_to_type"></param>
		/// <returns></returns>
		public TObject CastTo(TType cast_to_type) {
			Object obj = Object;
			return CreateAndCastFromObject(cast_to_type, obj);
		}



		public override string ToString() {
			return IsNull ? "NULL" : Object.ToString();
		}

		void IDeserializationCallback.OnDeserialization(object sender) {
			if (ob is string)
				ob = StringObject.FromString((string) ob);
		}
	}
}