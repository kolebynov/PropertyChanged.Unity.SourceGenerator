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
public class OnAnyPropertyChangedTests : TestsBase
{
    private static readonly CSharpSyntaxVisitor<SyntaxNode?>[] rewriters = new CSharpSyntaxVisitor<SyntaxNode?>[]
    {
        RemovePropertiesRewriter.Instance, RemoveDocumentationRewriter.Instance,
    };

    [Test]
    public void GeneratesParameterlessWhenAlsoGeneratingRaisePropertyChanged()
    {
        string input = """
            public partial class SomeViewModel
            {
                [Notify] string _foo;
                private void OnAnyPropertyChanged(string propertyName) { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesOldAndNewWhenAlsoGeneratingRaisePropertyChanged()
    {
        string input = """
            public partial class SomeViewModel
            {
                [Notify] string _foo;
                private void OnAnyPropertyChanged(string propertyName, object oldValue, object newValue) { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesParameterlessWhenOverridingParameterless()
    {
        string input = """
            using System.ComponentModel;
            public partial class Base
            {
                public event PropertyChangedEventHandler PropertyChanged;
                protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            public partial class Derived : Base
            {
                [Notify] private int _foo;
                private void OnAnyPropertyChanged(string name) { }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }

    [Test]
    public void GeneratesParameterlessWhenOverridingOldAndNew()
    {
        string input = """
            using System.ComponentModel;
            public partial class Base
            {
                public event PropertyChangedEventHandler PropertyChanged;
                protected virtual void OnPropertyChanged(string propertyName, object o, object n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            public partial class Derived : Base
            {
                [Notify] private int _foo;
                private void OnAnyPropertyChanged(string name) { }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }

    [Test]
    public void GeneratesOldAndNewWhenOverridingParameterless()
    {
        string input = """
            using System.ComponentModel;
            public partial class Base
            {
                public event PropertyChangedEventHandler PropertyChanged;
                protected virtual void OnPropertyChanged(PropertyChangedEventArgs ea) => PropertyChanged?.Invoke(this, ea);
            }
            public partial class Derived : Base
            {
                [Notify] private int _foo;
                private void OnAnyPropertyChanged(string name, object o, object n) { }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters).HasDiagnostics(
            // (10,18): Warning INPC025: The OnAnyPropertyChanged method has 'oldValue' and 'newValue' parameters, but the 'OnPropertyChanged' method defined in a base class does not. Please add these parameters to 'OnPropertyChanged'
            // OnAnyPropertyChanged
            Diagnostic("INPC025", @"OnAnyPropertyChanged").WithLocation(10, 18)
        ));
    }

    [Test]
    public void GeneratesOldAndNewWhenOverridingOldAndNew()
    {
        string input = """
            using System.ComponentModel;
            public partial class Base
            {
                public event PropertyChangedEventHandler PropertyChanged;
                protected virtual void OnPropertyChanged(PropertyChangedEventArgs ea, object o, object n) => PropertyChanged?.Invoke(this, ea);
            }
            public partial class Derived : Base
            {
                [Notify] private int _foo;
                private void OnAnyPropertyChanged(string name, object o, object n) { }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }

    [Test]
    public void GeneratesForShadowedMethod()
    {
        string input = """
            public partial class Base
            {
                [Notify] private string _foo;
                protected void OnAnyPropertyChanged(string propertyName) { }
            }
            public partial class Derived : Base
            {
                [Notify] private string _bar;
                protected new void OnAnyPropertyChanged(string propertyName) { }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }

    [Test]
    public void DoesNotGenerateForOverriddenMethod()
    {
        string input = """
            public partial class Base
            {
                [Notify] private string _foo;
                protected virtual void OnAnyPropertyChanged(string propertyName) { }
            }
            public partial class Derived : Base
            {
                [Notify] private string _bar;
                protected override void OnAnyPropertyChanged(string propertyName) { }
            }
            """;

        this.AssertThat(input, It.HasFile("Base", rewriters)
            .HasFile("Derived", rewriters));
    }

    [Test]
    public void RaisesIfSignatureNotRecognise()
    {
        string input = """
            public partial class SomeViewModel
            {
                [Notify] private int _foo;
                private void OnAnyPropertyChanged() { }
            }
            """;

        this.AssertThat(input, It.HasDiagnostics(
            // (4,18): Warning INPC024: Found one or more OnAnyPropertyChanged methods, but none had the correct signature, or were inaccessible. Skipping
            // OnAnyPropertyChanged
            Diagnostic("INPC024", @"OnAnyPropertyChanged").WithLocation(4, 18)));
    }

    [Test]
    public void RaisesIfCannotGenerateRaisePropertyChangedMethod()
    {
        string input = """
            using System.ComponentModel;
            public partial class Base : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler PropertyChanged;
                protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            public partial class Derived : Base
            {
                [Notify] private int _foo;
                private void OnAnyPropertyChanged(string name) { }
            }
            """;

        this.AssertThat(input, It.HasDiagnostics(
            // (5,20): Warning INPC022: Method 'OnPropertyChanged' is non-virtual. Functionality such as dependencies on base properties will not work. Please make this method virtual
            // OnPropertyChanged
            Diagnostic("INPC022", @"OnPropertyChanged").WithLocation(5, 20),

            // (10,18): Warning INPC026: OnAnyPropertyChanged method will not be called because the method to raise PropertyChanged events 'OnPropertyChanged' cannot defined or overridden by the source generator
            // OnAnyPropertyChanged
            Diagnostic("INPC026", @"OnAnyPropertyChanged").WithLocation(10, 18)
        ));
    }
}
