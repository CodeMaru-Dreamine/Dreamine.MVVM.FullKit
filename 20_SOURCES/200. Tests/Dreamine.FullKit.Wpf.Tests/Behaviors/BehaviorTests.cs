using System.Windows;
using Dreamine.MVVM.Behaviors.Core.Base;
using Dreamine.MVVM.Behaviors.Core.Interfaces;
using Dreamine.MVVM.Behaviors.Wpf.Interactivity;

namespace Dreamine.FullKit.Wpf.Tests.Behaviors;

public sealed class BehaviorTests
{
    [Fact]
    public void Behavior_AttachAndDetachUpdateAssociatedObjectAndHooks()
    {
        var target = new DependencyObject();
        var behavior = new TestBehavior();

        behavior.Attach(target);

        Assert.Same(target, behavior.AssociatedObject);
        Assert.Equal(1, behavior.AttachedCount);

        behavior.Detach();

        Assert.Null(behavior.AssociatedObject);
        Assert.Equal(1, behavior.DetachedCount);
    }

    [Fact]
    public void BehaviorCollection_AttachesAndDetachesChildBehaviors()
    {
        var target = new DependencyObject();
        var behavior = new TestCollectionBehavior();
        var collection = new BehaviorCollection { behavior };

        collection.Attach(target);
        collection.Detach();

        Assert.Equal(1, behavior.AttachedCount);
        Assert.Equal(1, behavior.DetachedCount);
    }

    [Fact]
    public void BehaviorCollection_AttachesGenericAttachedObjectsThroughSingleContract()
    {
        var target = new DependencyObject();
        var behavior = new TestBehavior();
        var collection = new BehaviorCollection { behavior };

        collection.Attach(target);
        collection.Detach();

        Assert.Equal(1, behavior.AttachedCount);
        Assert.Equal(1, behavior.DetachedCount);
    }

    [Fact]
    public void Interaction_GetBehaviorsCreatesEmptyCollectionWithoutDefaultBehavior()
    {
        var target = new DependencyObject();

        BehaviorCollection collection = Interaction.GetBehaviors(target);

        Assert.Empty(collection);
        Assert.DoesNotContain(collection, item => item is WindowDragBehavior);
        Assert.Same(collection, Interaction.GetBehaviors(target));
    }

    [Fact]
    public void Interaction_SetBehaviorsAttachesExplicitCollection()
    {
        var target = new DependencyObject();
        var behavior = new TestCollectionBehavior();
        var collection = new BehaviorCollection { behavior };

        Interaction.SetBehaviors(target, collection);

        Assert.Same(collection, Interaction.GetBehaviors(target));
        Assert.Equal(1, behavior.AttachedCount);
    }

    private sealed class TestBehavior : Behavior<DependencyObject>
    {
        public int AttachedCount { get; private set; }

        public int DetachedCount { get; private set; }

        protected override void OnAttached()
        {
            AttachedCount++;
        }

        protected override void OnDetaching()
        {
            DetachedCount++;
        }
    }

    private sealed class TestCollectionBehavior : DependencyObject, IBehavior
    {
        public DependencyObject AssociatedObject { get; private set; } = null!;

        public int AttachedCount { get; private set; }

        public int DetachedCount { get; private set; }

        public void Attach(DependencyObject dependencyObject)
        {
            AssociatedObject = dependencyObject;
            AttachedCount++;
        }

        public void Detach()
        {
            AssociatedObject = null!;
            DetachedCount++;
        }
    }
}
