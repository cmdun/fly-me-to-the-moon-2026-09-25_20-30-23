#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FlyMeToTheMoon;
using FlyMeToTheMoon.Instruments;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PianoPlayModeTests
{
    private PlayerController player;
    private PianoCollection collection;
    private PianoCollectionPanel panel;
    private PianoPickup[] pickups;
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    private readonly List<PianoPieceDefinition> temporaryDefinitions = new List<PianoPieceDefinition>();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousBackground = InputSystem.settings.backgroundBehavior;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Instruments/Piano/PianoCollectionDemo.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        keyboard = InputSystem.AddDevice<Keyboard>();
        player = Object.FindAnyObjectByType<PlayerController>();
        collection = player.GetComponent<PianoCollection>();
        panel = Object.FindAnyObjectByType<PianoCollectionPanel>();
        pickups = Object.FindObjectsByType<PianoPickup>();
        yield return new WaitForSeconds(0.2f);
        Assert.That(collection.Count, Is.Zero);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        foreach (var definition in temporaryDefinitions) Object.Destroy(definition);
        temporaryDefinitions.Clear();
        yield return null;
    }

    private PianoPickup Pickup(string id) => pickups.Single(p => p.piece.pieceId == "piano.demo." + id);

    private IEnumerator Touch(PianoPickup pickup)
    {
        player.Body.position = pickup.transform.position;
        player.Body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        float end = Time.time + 1f;
        while (pickup.gameObject.activeSelf && Time.time < end) yield return new WaitForFixedUpdate();
        Assert.That(pickup.gameObject.activeSelf, Is.False, "A player trigger overlap must collect the score.");
    }

    [UnityTest]
    public IEnumerator TouchScorePlaysSoundAndAddsReplayButton()
    {
        int events = 0;
        collection.PieceCollected += _ => events++;
        yield return Touch(Pickup("c4"));
        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(events, Is.EqualTo(1));
        Assert.That(collection.Contains("piano.demo.c4"), Is.True);
        Assert.That(player.GetComponent<PianoAudioPlayer>().LastPlayedClip, Is.EqualTo(Pickup("c4").piece.sound));
        Assert.That(player.GetComponent<AudioSource>().isPlaying, Is.True);
        Assert.That(panel.replayButtons[0].interactable, Is.True);
        Assert.That(panel.replayLabels[0].text, Is.EqualTo("Piano C4"));
    }

    [UnityTest]
    public IEnumerator DifferentMoonFragmentsPlayTheirAssignedSounds()
    {
        yield return Touch(Pickup("e4"));
        Assert.That(player.GetComponent<PianoAudioPlayer>().LastPlayedClip, Is.EqualTo(Pickup("e4").piece.sound));
        yield return Touch(Pickup("g4"));
        Assert.That(player.GetComponent<PianoAudioPlayer>().LastPlayedClip, Is.EqualTo(Pickup("g4").piece.sound));
        Assert.That(collection.Count, Is.EqualTo(2));
        Assert.That(Pickup("e4").piece.sound, Is.Not.EqualTo(Pickup("g4").piece.sound));
    }

    [UnityTest]
    public IEnumerator DuplicatePlacementsDoNotAwardOrPlayTwice()
    {
        int sounds = 0;
        collection.PiecePlayed += _ => sounds++;
        var original = Pickup("c4");
        yield return Touch(original);
        var duplicate = Object.Instantiate(original.gameObject).GetComponent<PianoPickup>();
        duplicate.transform.position = player.transform.position;
        duplicate.gameObject.SetActive(true);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(duplicate.gameObject.activeSelf, Is.False);
        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(sounds, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ReplayButtonPlaysOwnedSoundWithoutCollectingAgain()
    {
        yield return Touch(Pickup("c4"));
        yield return new WaitForSeconds(1f);
        Assert.That(player.GetComponent<AudioSource>().isPlaying, Is.False);
        int plays = 0;
        collection.PiecePlayed += _ => plays++;
        ExecuteEvents.Execute(panel.replayButtons[0].gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);
        yield return null;
        Assert.That(plays, Is.EqualTo(1));
        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(player.GetComponent<AudioSource>().isPlaying, Is.True);
        Assert.That(panel.status.text, Is.EqualTo("Playing: Piano C4"));
    }

    [UnityTest]
    public IEnumerator UncollectedPiecesCannotReplayAndRespawnKeepsCollection()
    {
        Assert.That(collection.Replay("piano.demo.e4"), Is.False);
        Assert.That(player.GetComponent<AudioSource>().isPlaying, Is.False);
        yield return Touch(Pickup("c4"));
        Object.FindAnyObjectByType<PlanetManager>().ResetPlayer();
        yield return new WaitForSeconds(1f);
        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(Pickup("c4").gameObject.activeSelf, Is.False);
        Assert.That(collection.Replay("piano.demo.c4"), Is.True);
        yield return null;
        Assert.That(player.GetComponent<AudioSource>().isPlaying, Is.True);
    }

    [UnityTest]
    public IEnumerator CollectionPagesBeyondFourFragmentsWithoutDependingOnMoons()
    {
        for (int i = 0; i < 6; i++)
        {
            var piece = ScriptableObject.CreateInstance<PianoPieceDefinition>();
            piece.pieceId = "test.fragment." + i;
            piece.displayName = "Fragment " + i;
            piece.sound = Pickup("c4").piece.sound;
            temporaryDefinitions.Add(piece);
            Assert.That(collection.TryCollect(piece), Is.True);
        }
        yield return null;
        Assert.That(panel.replayLabels[0].text, Is.EqualTo("Fragment 4"));
        Assert.That(panel.previousButton.interactable, Is.True);
        Assert.That(panel.nextButton.interactable, Is.False);
        PianoPieceDefinition played = null;
        collection.PiecePlayed += piece => played = piece;
        panel.replayButtons[1].onClick.Invoke();
        Assert.That(played, Is.EqualTo(temporaryDefinitions[5]));
        panel.previousButton.onClick.Invoke();
        Assert.That(panel.replayLabels[0].text, Is.EqualTo("Fragment 0"));
        Assert.That(panel.nextButton.interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator NonPlayerOverlapDoesNotCollect()
    {
        var visitor = new GameObject("Non-player test body", typeof(Rigidbody2D), typeof(CircleCollider2D));
        visitor.GetComponent<Rigidbody2D>().gravityScale = 0;
        visitor.transform.position = Pickup("c4").transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(Pickup("c4").gameObject.activeSelf, Is.True);
        Assert.That(collection.Count, Is.Zero);
        Object.Destroy(visitor);
    }
}

#endif
