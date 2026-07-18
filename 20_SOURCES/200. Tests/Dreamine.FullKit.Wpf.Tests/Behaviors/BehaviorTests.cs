using System.Windows;
using Dreamine.MVVM.Behaviors.Core.Base;
using Dreamine.MVVM.Behaviors.Core.Interfaces;
using Dreamine.MVVM.Behaviors.Wpf.Interactivity;

namespace Dreamine.FullKit.Wpf.Tests.Behaviors;

/// <summary>
/// \if KO
/// <para>Behavior Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates behavior tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class BehaviorTests
{
    /// <summary>
    /// \if KO
    /// <para>Behavior Attach And Detach Update Associated Object And Hooks 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the behavior attach and detach update associated object and hooks operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Behavior Collection Attaches And Detaches Child Behaviors 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the behavior collection attaches and detaches child behaviors operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Behavior Collection Attaches Generic Attached Objects Through Single Contract 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the behavior collection attaches generic attached objects through single contract operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Interaction Get Behaviors Creates Empty Collection Without Default Behavior 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the interaction get behaviors creates empty collection without default behavior operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Interaction_GetBehaviorsCreatesEmptyCollectionWithoutDefaultBehavior()
    {
        var target = new DependencyObject();

        BehaviorCollection collection = Interaction.GetBehaviors(target);

        Assert.Empty(collection);
        Assert.DoesNotContain(collection, item => item is WindowDragBehavior);
        Assert.Same(collection, Interaction.GetBehaviors(target));
    }

    /// <summary>
    /// \if KO
    /// <para>Interaction Set Behaviors Attaches Explicit Collection 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the interaction set behaviors attaches explicit collection operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Test Behavior 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test behavior functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestBehavior : Behavior<DependencyObject>
    {
        /// <summary>
        /// \if KO
        /// <para>Attached Count 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the attached count value.</para>
        /// \endif
        /// </summary>
        public int AttachedCount { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Detached Count 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the detached count value.</para>
        /// \endif
        /// </summary>
        public int DetachedCount { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Attached 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Handles the attached event or state change.</para>
        /// \endif
        /// </summary>
        protected override void OnAttached()
        {
            AttachedCount++;
        }

        /// <summary>
        /// \if KO
        /// <para>Detaching 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Handles the detaching event or state change.</para>
        /// \endif
        /// </summary>
        protected override void OnDetaching()
        {
            DetachedCount++;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Test Collection Behavior 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test collection behavior functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestCollectionBehavior : DependencyObject, IBehavior
    {
        /// <summary>
        /// \if KO
        /// <para>Associated Object 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the associated object value.</para>
        /// \endif
        /// </summary>
        public DependencyObject AssociatedObject { get; private set; } = null!;

        /// <summary>
        /// \if KO
        /// <para>Attached Count 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the attached count value.</para>
        /// \endif
        /// </summary>
        public int AttachedCount { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Detached Count 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the detached count value.</para>
        /// \endif
        /// </summary>
        public int DetachedCount { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>대상 객체에 동작을 연결합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Attaches the behavior to a target object.</para>
        /// \endif
        /// </summary>
        /// <param name="dependencyObject">
        /// \if KO
        /// <para>동작 또는 연결 속성을 적용할 종속성 객체입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The dependency object to which the behavior or attached property applies.</para>
        /// \endif
        /// </param>
        public void Attach(DependencyObject dependencyObject)
        {
            AssociatedObject = dependencyObject;
            AttachedCount++;
        }

        /// <summary>
        /// \if KO
        /// <para>현재 대상 객체에서 동작을 분리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Detaches the behavior from its current target object.</para>
        /// \endif
        /// </summary>
        public void Detach()
        {
            AssociatedObject = null!;
            DetachedCount++;
        }
    }
}
