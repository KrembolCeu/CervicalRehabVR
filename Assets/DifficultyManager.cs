using UnityEngine;

/// <summary>
/// DifficultyManager.cs
///
/// Clase estática que persiste la dificultad seleccionada entre escenas.
/// No necesita MonoBehaviour; basta con llamar a SetDifficulty() y leer
/// SelectedDifficulty desde cualquier script en cualquier escena.
///
/// Valores válidos de dificultad:
///   "Facil"   - fácil
///   "Medio"   - medio
///   "Dificil" - difícil
///
/// Ejemplo de uso en el juego:
///   if (DifficultyManager.SelectedDifficulty == "Facil")
///       obstacleSpeed = 1f;
/// </summary>
public static class DifficultyManager
{
    private static string _selected = "Medio"; // valor por defecto

    /// <summary>Dificultad actualmente seleccionada.</summary>
    public static string SelectedDifficulty => _selected;

    /// <summary>
    /// Guarda la dificultad elegida. Llamado por GazeTimerButton al confirmarse.
    /// </summary>
    public static void SetDifficulty(string difficulty)
    {
        _selected = difficulty;
        Debug.Log($"[DifficultyManager] Dificultad seleccionada: {difficulty}");
    }
}
