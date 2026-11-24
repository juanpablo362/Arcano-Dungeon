# Arcano Dungeon

Juego de mazmorras en Unity 6 con Universal Render Pipeline.

El contenido del juego vive en `Assets/_ArcanoDungeon/`. `Assets/Plugins` y `Assets/ThirdParty` quedan reservados para código externo.

## Carpetas

| Ruta | Uso |
| --- | --- |
| `Art/Characters` | Arte de Elara, Lyra y Kaede |
| `Art/Environments` | Entornos de Forest, Catacombs y Castle |
| `Art/Materials`, `Textures`, `VFX` | Materiales, texturas y efectos |
| `Audio` | Música, efectos de sonido y mixers |
| `Scenes` | Bootstrap, menú, selección de personaje y niveles |
| `Features` | Menú, selección, jugador, combate, habilidades, enemigos, niveles, recompensas y resultados |
| `Core` | Flujo de juego, carga de escenas, eventos e interfaces |
| `Infrastructure` | Persistencia, audio e input |
| `Shared` | UI, prefabs y utilidades compartidas |
| `Settings` | Ajustes de input y rendering del juego |
| `Tests` | Pruebas de Edit Mode y Play Mode |

## Escenas

1. `Bootstrap`
2. `MainMenu`
3. `CharacterSelection`
4. `ForestLevel`
5. `CatacombsLevel`
6. `CastleLevel`

## Recorrido jugable

`MainMenu → CharacterSelection → Level1 → CatacombsLevel → CastleLevel`.

El tercer nivel, **Castillo Eclipse**, contiene siete salas: Atrio, Guardia, Armería,
Biblioteca, Galería, Capilla y Trono. Las rejas se cierran durante los encuentros.
Despejar la Armería y la Capilla rompe los dos sellos que bloquean el trono.
El Regente del Eclipse anuncia su nova con un círculo violeta; al derrotar a todos
los enemigos del trono se activa el portal de victoria.

Para abrirlo directamente: `Assets/_ArcanoDungeon/Scenes/CastleLevel.unity`.
El comando `Tools/Arcano/Build Castillo Eclipse` reconstruye esta escena desde sus
modelos y materiales; reemplaza el contenido de CastleLevel.

## Progreso y recursos de la partida

Los portales conservan puntaje, vida, escudo, estamina, bombas, mejoras apiladas y
el tiempo restante del arma temporal entre niveles. Una nueva partida o la muerte
limpian ese estado. Este estado corresponde a la sesión actual; no es un guardado
persistente al cerrar el juego.

`DungeonRuntimeAssets`, dentro de Resources, mantiene referencias serializadas a
efectos, modelos de recompensas, mímicos y animaciones de selección para incluirlos
en la exportación. Se actualiza antes de compilar y mediante
`Tools/Arcano/Rebuild Runtime Asset Catalog`.

La exportación excluye los shaders del paquete de inferencia de IA cuando no hay
modelos ni código del juego que lo utilice. Las herramientas de IA del editor
siguen instaladas. Si se añaden modelos o referencias a `Unity.InferenceEngine` /
`Unity.Sentis` en código del jugador, la exclusión se desactiva automáticamente.
