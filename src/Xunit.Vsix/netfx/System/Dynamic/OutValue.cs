
namespace System.Dynamic
{
    /// <summary>
    /// Allows output parameters to be passed to reflection dynamic.
    /// This support does not exist in C# 4.0 dynamic out of the box.
    /// </summary>
    abstract partial class OutValue
    {
        /// <summary>
        /// Creates a value setter delegating reference
        /// to be used as an output parameter when invoking the 
        /// dynamic object.
        /// </summary>
        /// <param name="setter">The value to pass as out to the dynamic invocation.</param>
        public static OutValue<T> Create<T>(Action<T> setter)
        {
            return new OutValue<T>(setter);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        internal abstract object Value { set; }
    }

    /// <summary>
    /// Allows output parameters to be passed to reflection dynamic.
    /// This support does not exist in C# 4.0 dynamic out of the box.
    /// </summary>
    partial class OutValue<T> : OutValue
    {
        Action<T> _setter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutValue&lt;T&gt;"/> class.
        /// </summary>
        public OutValue(Action<T> setter)
        {
            _setter = setter;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        internal override object Value
        {
            set { _setter((T)value); }
        }
    }
}
