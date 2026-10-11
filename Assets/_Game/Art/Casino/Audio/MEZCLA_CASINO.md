# Mezcla del Casino FOX

`CasinoFOX.mixer` tiene tres buses hijos de `Master`: `Musica`, `Efectos` y `Ambiente`. El constructor de la escena los asigna a todas las fuentes del casino. Las fuentes temporales de giro, parada, apuesta, premios y cajera van a `Efectos`; el bucle de la máquina también. `casino_music.wav` va a `Musica` y `casino_ambient.wav` a `Ambiente`.

Los buses empiezan a **0 dB**. Ganancias de fuente: música **0.35**, ambiente **0.40**, efectos **0.80** (y multiplicadores puntuales de 0.7–0.9 para parada, apuesta y fichas); el bucle de los rodillos usa **0.40**. Música y ambiente son 3D lineales, con alcance de 5–26 m; el audio del casino se atenúa fuera del local. Los efectos temporales son 3D lineales, con alcance de 2–20 m.

`casino_music.wav` es una pieza instrumental original sintetizada para este proyecto. No contiene material de terceros.

Al girar, `SlotMachineStation` imprime `CASINO_AUDIO giro=True musica=N efectos=N ambiente=N bucle=True`; los contadores indican fuentes cuyo `AudioSource.isPlaying` es verdadero y cuyo bus coincide. Este diagnóstico confirma el enrutamiento y estado de reproducción en Unity, **no** que una persona escuchó la mezcla. La escucha de la app compilada sigue pendiente y debe cubrir giro, tres paradas, premio normal, jackpot, cajera y ambiente sin tapar avisos.

Después de fusionar este cambio, el área C debe regenerar `BarrioResidencial.unity` con `CasinoFoxIntegration.IntegrateAllWithCasino` y subir la escena; este cambio no incluye el archivo generado.
