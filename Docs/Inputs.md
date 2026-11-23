# Inputs et Event Bus

## Fonctionnement

```
Clavier / manette
      │
PlayerInput (PlayerActionMap, map "Player")
      │
InputReader ── lit les actions de la map Player
      │
EventBus<MoveInputEvent>, EventBus<RotateInputEvent>...
      │
N'importe quel script du jeu abonné à ces événements
```

Les scripts de gameplay ne touchent jamais à l'Input System. Ils écoutent des événements.
Changer une touche ou passer à la manette ne demande donc aucune modification du gameplay.

| Fichier | Rôle |
|---|---|
| `Scripts/Runtime/Core/Events/IEvent.cs` | Interface que tout événement implémente |
| `Scripts/Runtime/Core/Events/EventBus.cs` | Le bus : `Subscribe`, `Unsubscribe`, `Publish` |
| `Scripts/Runtime/Core/Events/EventBusRegistry.cs` | Vide le bus à chaque lancement du Play Mode |
| `Scripts/Runtime/Input/InputEvents.cs` | Les événements d'input |
| `Scripts/Runtime/Input/InputReader.cs` | Traduit les actions en événements |
| `Assets/Settings/PlayerActionMap.inputactions` | Les actions et leurs touches |

Dans la scène `Game`, l'objet `PlayerInputsHandler` porte le `PlayerInput` et l'`InputReader`.

## Événements disponibles

| Événement | Touche | Champ | Publié quand |
|---|---|---|---|
| `MoveInputEvent` | A / D, Gauche / Droite | `Direction` : `-1` (gauche), `1` (droite), `0` (relâché) | La direction change |
| `DownInputEvent` | Espace, Bas | `IsPressed` : `true` / `false` | Appui puis relâchement |
| `RotateInputEvent` | R, Haut | aucun | Appui |
| `PauseInputEvent` | Échap | aucun | Appui |
| `MiscInputEvent` | J | aucun | Appui (réservé pour plus tard) |

Les touches lettres sont liées par position physique, sur la base d'un clavier QWERTY. Sur un clavier AZERTY :
A devient Q. D, R et J ne changent pas.

Un événement n'est publié que si la valeur change. Maintenir une touche n'envoie rien en boucle :
si le gameplay veut répéter un déplacement tant que la touche est tenue, c'est à lui de le faire.

## Écouter un événement

```csharp
using Tetris.Events;
using UnityEngine;

namespace Tetris {
    public class PieceController : MonoBehaviour {
        void OnEnable() {
            EventBus<MoveInputEvent>.Subscribe(OnMove);
            EventBus<DownInputEvent>.Subscribe(OnDown);
        }

        void OnDisable() {
            EventBus<MoveInputEvent>.Unsubscribe(OnMove);
            EventBus<DownInputEvent>.Unsubscribe(OnDown);
        }

        void OnMove(MoveInputEvent e) {
            Debug.Log($"Direction : {e.Direction}");
        }

        void OnDown(DownInputEvent e) {
            Debug.Log($"Down : {e.IsPressed}");
        }
    }
}
```

Règles :

- Toujours se désabonner dans `OnDisable`. Sinon un objet détruit continue de recevoir les événements.
- S'abonner avec une méthode (`OnMove`), pas avec une lambda. Une lambda ne peut pas être
  désabonnée, sauf si on la garde dans une variable.
- Si un abonné lève une exception, elle s'affiche dans la console et les autres abonnés
  reçoivent quand même l'événement.

## Modifier les touches

1. Ouvrir `Assets/Settings/PlayerActionMap.inputactions`.
2. Dans la map `Player`, modifier ou ajouter des bindings sur l'action voulue.
3. Sauvegarder (*Save Asset*).

Aucun code à toucher. Pour `Move` (un *1D Axis*), la touche `Negative` donne `Direction = -1`
et la touche `Positive` donne `Direction = 1`.

Ne pas renommer la map `Player` ni ses actions sans mettre à jour les constantes
en haut de `InputReader.cs`. Sinon une erreur apparaît au lancement.

## Ajouter un input

Exemple avec une réserve de pièce (Hold).

**1. L'action.** Dans `PlayerActionMap`, map `Player`, ajouter une action `Hold` de type *Button*,
avec ses touches.

**2. L'événement**, dans `InputEvents.cs` :

```csharp
public readonly struct HoldInputEvent : IEvent { }
```

Un bouton qui ne sert qu'à l'appui n'a pas besoin de champ. S'il faut savoir quand il est relâché,
ajouter un `bool IsPressed` comme dans `DownInputEvent`.

**3. La lecture**, dans `InputReader.cs` :

```csharp
const string HoldActionName = "Player/Hold";

InputAction _holdAction;

// Awake
_holdAction = actions.FindAction(HoldActionName, throwIfNotFound: true);

// OnEnable
_holdAction.performed += HandleHold;

// OnDisable
_holdAction.performed -= HandleHold;

void HandleHold(InputAction.CallbackContext context) {
    EventBus<HoldInputEvent>.Publish(new HoldInputEvent());
}
```

**4. L'utilisation** : s'abonner à `HoldInputEvent` comme dans l'exemple plus haut.

## Créer un événement qui n'est pas un input

Le bus sert à tout le jeu, pas seulement aux inputs. Par exemple, quand des lignes sont effacées :

```csharp
public readonly struct LinesClearedEvent : IEvent {
    public readonly int Count;

    public LinesClearedEvent(int count) {
        Count = count;
    }
}

// Là où les lignes sont effacées
EventBus<LinesClearedEvent>.Publish(new LinesClearedEvent(4));
```

Le score, le son et l'UI peuvent alors s'y abonner sans se connaître entre eux.

Pour les événements qui ne sont pas des inputs, créer le fichier dans le dossier de la
fonctionnalité concernée (`Gameplay/`, `UI/`...).

## Tests

`Tests/EditMode/EventBusTests.cs` vérifie le bus. Pour les lancer, ouvrir
*Window > General > Test Runner*, onglet *EditMode*, puis *Run All*.
