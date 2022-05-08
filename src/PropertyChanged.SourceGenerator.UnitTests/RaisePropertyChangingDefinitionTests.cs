﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using PropertyChanged.SourceGenerator.UnitTests.Framework;

namespace PropertyChanged.SourceGenerator.UnitTests;

[TestFixture]
public class RaisePropertyChangingDefinitionTests : TestsBase
{
    private static readonly CSharpSyntaxVisitor<SyntaxNode?>[] rewriters = new CSharpSyntaxVisitor<SyntaxNode?>[]
    {
        RemoveInpcMembersRewriter.Instance, RemovePropertiesRewriter.Instance
    };


    [Test]
    public void DoesNothingIfTypeDoesNotImplementInterface()
    {
        string input = @"
public partial class SomeViewModel
{
    [Notify]
    private string _foo;
}";

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesEventAndRaisePropertyChangedIfNotDefined()
    {
        string input = @"
using System.ComponentModel;
public partial class SomeViewModel : INotifyPropertyChanging
{
    [Notify]
    private string _foo;
}";

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesRaisePropertyChangingIfNotDefined()
    {
        string input = @"
using System.ComponentModel;
public partial class SomeViewModel : INotifyPropertyChanging
{
    public event PropertyChangingEventHandler PropertyChanging;
    [Notify]
    private string _foo;
}";

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void RaisesIfEventButNoRaiseMethodOnBaseClass()
    {
        string input = @"
using System.ComponentModel;
public class Base : INotifyPropertyChanging
{
    public event PropertyChangingEventHandler PropertyChanging;
}
public partial class Derived : Base
{
    [Notify]
    private string _foo;
}";

        this.AssertThat(input, It.HasDiagnostics(
            // (7,22): Warning INPC0028: Could not find any suitable methods to raise the PropertyChanging event defined on a base class
            // Derived
            Diagnostic("INPC0028", @"Derived").WithLocation(7, 22)
        ));
    }

    [Test]
    public void RaisesIfMethodOnBaseClassIsPrivate()
    {
        string input = @"
using System.ComponentModel;
public class Base : INotifyPropertyChanging
{
    public event PropertyChangingEventHandler PropertyChanging;
    private void OnPropertyChanging(string name) => this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
}
public partial class Derived : Base
{
    [Notify]
    private string _foo;
}";

        this.AssertThat(input, It.HasDiagnostics(
            // (8,22): Warning INPC029: Found one or more methods called 'OnPropertyChanging' to raise the PropertyChanging event, but they had an unrecognised signatures or were inaccessible. No PropertyChanging events will be raised from this type
            // Derived
            Diagnostic("INPC029", @"Derived").WithLocation(8, 22)
        ));
    }

    [Test]
    public void RaisesIfNonVirtualBaseMethodAndOverrideRequired()
    {
        string input = @"
using System.ComponentModel;
public class Base : INotifyPropertyChanging
{
    public event PropertyChangingEventHandler PropertyChanging;
    protected void OnPropertyChanging(string name) => this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
}
public partial class Derived : Base
{
    [Notify, DependsOn(""Bar"")]
    private string _foo;
}";

        this.AssertThat(input, It.HasFile("Derived", rewriters).HasDiagnostics(
            // (6,20): Warning INPC022: Method 'OnPropertyChanging' is non-virtual. Functionality such as dependencies on base properties will not work. Please make this method virtual
            // OnPropertyChanging
            Diagnostic("INPC022", @"OnPropertyChanging").WithLocation(6, 20)
        ));
    }

//    [Test]
//    public void DefinesVirtual()
//    {
//        string input = @"
//using System.ComponentModel;
//public partial class Derived : INotifyPropertyChanging
//{
//    [Notify, DependsOn(""Foo"")] private string _bar;
//}";

//        this.AssertThat(input, It.HasFile("Derived", rewriters));
//    }

    //    [Test]
    //    public void DefinesOverrideStringNoOldAndNew()
    //    {
    //        string input = @"
    //using System.ComponentModel;
    //public class Base : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected virtual void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    //}
    //public partial class Derived : Base
    //{
    //    [Notify, DependsOn(""Foo"")] private string _bar;
    //}";

    //        this.AssertThat(input, It.HasFile("Derived", RemovePropertiesRewriter.Instance));
    //    }

    //    [Test]
    //    public void DefinesOverrideStringOldAndNew()
    //    {
    //        string input = @"
    //using System.ComponentModel;
    //public class Base : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected virtual void OnPropertyChanged(string name, object oldValue, object newValue) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    //}
    //public partial class Derived : Base
    //{
    //    [Notify, DependsOn(""Foo"")] private string _bar;
    //}";

    //        this.AssertThat(input, It.HasFile("Derived", RemovePropertiesRewriter.Instance));
    //    }

    //    [Test]
    //    public void DefinesOverrideEventArgsNoOldAndNew()
    //    {
    //        string input = @"
    //using System.ComponentModel;
    //public class Base : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args) => this.PropertyChanged?.Invoke(this, args);
    //}
    //public partial class Derived : Base
    //{
    //    [Notify, DependsOn(""Foo"")] private string _bar;
    //}";

    //        this.AssertThat(input, It.HasFile("Derived", RemovePropertiesRewriter.Instance));
    //    }

    //    [Test]
    //    public void DefinesOverrideEventArgsOldAndNew()
    //    {
    //        string input = @"
    //using System.ComponentModel;
    //public class Base : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args, object oldValue, object newValue) => this.PropertyChanged?.Invoke(this, args);
    //}
    //public partial class Derived : Base
    //{
    //    [Notify, DependsOn(""Foo"")] private string _bar;
    //}";

    //        this.AssertThat(input, It.HasFile("Derived", RemovePropertiesRewriter.Instance));
    //    }

    //    [Test]
    //    public void CallsOnPropertyNameChangedNoOldAndNew()
    //    {
    //        string input = @"
    //using System.ComponentModel;
    //public class Base : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args, object oldValue, object newValue) => this.PropertyChanged?.Invoke(this, args);
    //}
    //public partial class Derived : Base
    //{
    //    [Notify, DependsOn(""Foo"")] private string _bar;
    //    private void OnBarChanged() { }
    //}";

    //        this.AssertThat(input, It.HasFile("Derived", RemovePropertiesRewriter.Instance));
    //    }

    //    [Test]
    //    public void CallsOnPropertyNameChangedOldAndNew()
    //    {
    //        string input = @"
    //using System.ComponentModel;
    //public class Base : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args, object oldValue, object newValue) => this.PropertyChanged?.Invoke(this, args);
    //}
    //public partial class Derived : Base
    //{
    //    [Notify, DependsOn(""Foo"")] private string _bar;
    //    private void OnBarChanged(string oldValue, string newValue) { }
    //}";

    //        this.AssertThat(input, It.HasFile("Derived", RemovePropertiesRewriter.Instance));
    //    }

    //    [TestCase("public")]
    //    [TestCase("protected internal")]
    //    [TestCase("protected")]
    //    [TestCase("internal")]
    //    [TestCase("private protected")]
    //    public void CopiesBaseMethodAccessibility(string accessibility)
    //    {
    //        string input = @$"
    //using System.ComponentModel;
    //public class Base : INotifyPropertyChanged
    //{{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    {accessibility} virtual void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    //}}
    //public partial class Derived : Base
    //{{
    //    [Notify, DependsOn(""Foo"")] private string _bar;
    //}}";

    //        this.AssertThat(input, It.HasFile("Derived", RemovePropertiesRewriter.Instance));
    //    }

    //    [Test]
    //    public void HandlesSealedClass()
    //    {
    //        string input = @"
    //public sealed partial class SomeViewModel
    //{
    //    [Notify] string _foo;
    //}";

    //        this.AssertThat(input, It.HasFile("SomeViewModel", RemovePropertiesRewriter.Instance));
    //    }
}
