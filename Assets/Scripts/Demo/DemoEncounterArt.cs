using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Skins encounter geometry, retaining its original transforms, hit targets and state.</summary>
    public sealed class DemoEncounterArt : MonoBehaviour
    {
        public SpriteRenderer Sprite { get; private set; }
        DemoGame game; PrototypeShape shape; string kind;
        float hitUntil; Vector3 lastPosition, baseScale;
        int ringCount;
        public bool Build(DemoGame owner, PrototypeShape source)
        {
            game = owner; shape = source; kind = source.name;
            string key; float height, offset = 0;
            switch (kind)
            {
                case "Sleeping giant": key = "giant/0"; height = 1.65f; offset = -.45f; break;
                case "Guarded page": key = "prop/page"; height = 1.15f; offset = -.45f; break;
                case "BELL": key = "01/object/0"; height = 1.5f; offset = -.95f; break;
                case "Echo gate": case "Sanctuary gate": key = "prop/gate"; height = shape.size.y; offset = -height * .5f; break;
                case "Echo plate": case "Duet plate": key = "03/object/0"; height = .23f; offset = -.075f; break;
                case "Recorded ghost": key = "hero/0"; height = 1.43f; offset = -.29f; break;
                case "High-wave shelter": key = "prop/arch"; height = 1.75f; offset = -.95f; break;
                case "Damaged shrine": key = "prop/crystals"; height = 1.25f; offset = -.3f; break;
                case "Low wave": case "High wave": key = "01/fx/4"; height = shape.size.y; break;
                case "Storm warning": key = "03/fx/0"; height = .3f; break;
                case "Gate lever": key = "09/object/0"; height = .8f; offset = -.5f; break;
                case "Sanctuary": key = "prop/garden"; height = 1.15f; offset = -.04f; break;
                case "Thorn patch": case "Noisy gravel": key = "prop/crystals"; height = kind == "Thorn patch" ? .35f : .18f; offset = -.07f; break;
                case "Flute call": key = "01/fx/4"; height = 1.2f; break;
                case "Musical spark": key = "02/fx/3"; height = .4f; break;
                case "Stabilizing shrine": case "Spark receiver": key = "prop/altar"; height = .95f; offset = -.3f; break;
                case "RECEIVER": key = "prop/crystals"; height = .65f; offset = -.3f; break;
                default:
                    if (kind.StartsWith("SWITCH")) { key = "prop/crystals"; height = .65f; offset = -.3f; }
                    else if (kind.StartsWith("Singer")) { key = "06/object/0"; height = .68f; offset = -.36f; }
                    else if (kind.StartsWith("Relay gate")) { key = "02/fx/3"; height = .2f; }
                    else return false;
                    break;
            }
            PixelArtLibrary.HideMeshes(transform);
            Sprite = PixelArtLibrary.Create(transform, "Encounter pixel art", key, kind == "Recorded ghost" ? 18 : 5);
            PixelArtLibrary.Height(Sprite, height);
            Sprite.transform.localPosition = new Vector3(0, offset, -.1f);
            if (kind.EndsWith("gate")) PixelArtLibrary.Size(Sprite, new Vector2(.8f, height));
            if (kind == "High-wave shelter") PixelArtLibrary.Size(Sprite, new Vector2(2.15f, height));
            if (kind.EndsWith("plate")) PixelArtLibrary.Size(Sprite, new Vector2(1.2f, height));
            baseScale = Sprite.transform.localScale; lastPosition = transform.position;
            if (kind == "BELL")
            {
                var receiver = GetComponent<EncounterShotTarget>();
                var hit = receiver.Hit;
                receiver.Hit = echo =>
                {
                    hit?.Invoke(echo); hitUntil = Time.time + .9f; ringCount++;
                    game.Art.Effect("01", transform.position, .7f, transform.rotation);
                };
            }
            return true;
        }
        void LateUpdate()
        {
            if (Sprite == null || game == null || Time.timeScale == 0) return;
            var encounter = game.Challenge.Encounter;
            switch (kind)
            {
                case "BELL":
                    Sprite.sprite = PixelArtLibrary.Get("01/object/" + (Time.time < hitUntil ? Mathf.Clamp(2 + (int)((.9f - hitUntil + Time.time) * 5), 2, 5) : ringCount > 0 ? 5 : 0));
                    break;
                case "Sleeping giant":
                    var giant = (GiantEncounter)encounter;
                    Sprite.sprite = PixelArtLibrary.Get("giant/" + (giant.Awareness > .65f ? 2 : giant.LureRemaining > 0 || giant.Awareness > .25f ? 1 : 0));
                    break;
                case "Echo gate": case "Sanctuary gate":
                    bool open = !GetComponent<BoxCollider2D>().enabled;
                    Sprite.transform.localRotation = Quaternion.RotateTowards(Sprite.transform.localRotation, Quaternion.Euler(0, 0, open ? -82 : 0), Time.deltaTime * 650);
                    Sprite.color = open ? new Color(.7f, 1, .85f, .75f) : Color.white;
                    break;
                case "Echo plate": case "Duet plate":
                    bool down = shape.color == Color.white;
                    Sprite.sprite = PixelArtLibrary.Get("03/object/" + (down ? 2 : 0));
                    Sprite.transform.localScale = new Vector3(baseScale.x, baseScale.y * (down ? .65f : 1), 1);
                    Sprite.color = down ? Color.white : kind == "Echo plate" ? new Color(.6f, 1, 1) : new Color(1, .85f, .45f);
                    break;
                case "Recorded ghost":
                    Vector3 movement = transform.position - lastPosition;
                    bool walking = movement.sqrMagnitude > .00001f;
                    if (walking) Sprite.flipX = Vector3.Dot(movement, transform.right) < 0;
                    Sprite.sprite = PixelArtLibrary.Get(walking ? "03/actor/" + (int)(Time.time * 10) % 6 : "hero/0");
                    Sprite.color = new Color(.45f, .95f, 1, .48f); break;
                case "Low wave": case "High wave": case "Flute call":
                    Sprite.color = kind == "Low wave" ? new Color(1, .7f, .35f) : kind == "High wave" ? new Color(.85f, .65f, 1) : new Color(.6f, 1, 1, .65f);
                    break;
                case "Thorn patch": case "Noisy gravel": Sprite.color = new Color(1, .4f, .4f); break;
                case "Damaged shrine":
                    Sprite.color = Color.Lerp(new Color(.65f, .65f, .8f), Color.white, encounter.Stage / 3f); break;
                default:
                    if (kind.StartsWith("Singer"))
                    {
                        var singer = ((ShepherdEncounter)encounter).Creatures[kind[kind.Length - 1] - '0'];
                        bool moving = (transform.position - lastPosition).sqrMagnitude > .00001f;
                        int frame = singer.Rescued ? 5 : moving ? 2 + (int)(Time.time * 7) % 3 : 0;
                        Sprite.sprite = PixelArtLibrary.Get("06/object/" + frame);
                        Sprite.color = singer.Fear > 0 ? new Color(1, .5f, .5f) : !singer.Awake ? new Color(.5f, .55f, .7f) : Color.Lerp(Color.white, shape.color, .4f);
                        if (moving) Sprite.flipX = Vector3.Dot(transform.position - lastPosition, transform.right) < 0;
                    }
                    else if (kind.StartsWith("SWITCH"))
                    {
                        var storm = (StormEncounter)encounter; int index = kind.EndsWith("1") ? 0 : 1;
                        Sprite.color = storm.Repaired[index] ? DemoGame.Accent : storm.Exposed ? new Color(1, .95f, .65f) : new Color(.38f, .38f, .5f);
                    }
                    else if (kind.StartsWith("Relay gate")) Sprite.color = shape.color;
                    break;
            }
            lastPosition = transform.position;
        }
    }
}
