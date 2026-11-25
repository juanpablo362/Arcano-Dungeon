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

## Asesina chibi

La **Asesina** aparece en el selector de personajes (tecla **5**). Su prefab jugable
está en `Assets/_ArcanoDungeon/Resources/Characters/AssassinPlayer.prefab`.
Incluye modelo Humanoid con materiales URP, locomoción, salto, sigilo, un combo de
tres golpes y animaciones de lanzamiento.

- **Clic izquierdo:** combo de tres golpes.
- **Clic derecho:** cuchillo envenenado apuntado al centro de la pantalla. Causa
  20 de daño directo y cuatro pulsos de veneno de 6; el lanzamiento tiene una
  espera de 0,55 segundos. Las paredes detienen el proyectil.
- **Mantener C/Ctrl:** cobertura de sombras. Los enemigos pierden el objetivo y
  cancelan sus ataques, incluidas las novas del jefe. Se puede atacar desde el sigilo.
- **E:** Filo doble. **Alt:** correr. **Shift:** dash. **Espacio:** saltar.

`Assets/_ArcanoDungeon/Scenes/AssassinTraining.unity` permite probarla contra un
blanco de práctica. El tamaño del personaje aumenta un 15 % mediante
`AssassinSizing`, con cápsula, cámara, dagas y sombras proporcionadas.

Las herramientas están en `Tools/Arcano/Assassin`. La configuración de habilidades
se actualiza con `Update poison and shadow abilities`; `Validate poison and stealth`
comprueba proyectiles, veneno, detección y cancelación de ataques.

## Barras compartidas

`Level1`, `CatacombsLevel` y `CastleLevel` utilizan el mismo panel de **vida,
escudo y estamina**, con disposición y escalado de pantalla comunes. El recurso
es `Assets/_ArcanoDungeon/Resources/UI/PlayerVitalsHUD.prefab`.
`PlayerVitalsHud` lo instala al cargar cada nivel y sustituye las barras antiguas
durante la partida. Los valores proceden de los componentes del jugador y se
conservan al atravesar portales.

`Tools/Arcano/Update shared HUD and assassin size` regenera la presentación.
`Tools/Arcano/Validate HUD in all three levels` recorre los tres niveles y comprueba
datos, transiciones, disposición, colisiones y encuadre.

Los resultados verificados están en [Docs/Validation](Docs/Validation): 29
comprobaciones de veneno y sigilo y 74 de barras y tamaño. Las salidas nuevas de
las herramientas se generan localmente en `AssassinValidation/`.
