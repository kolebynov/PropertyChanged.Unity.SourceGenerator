﻿partial class SomeViewModel
{
    public bool IsChanged
    {
        get => this._isChanged;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<bool>.Default.Equals(value, this._isChanged))
            {
                this._isChanged = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_IsChanged);
            }
        }
    }
    public string Foo
    {
        get => this._foo;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this._foo))
            {
                this._foo = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Foo);
                this.IsChanged = true;
            }
        }
    }
}