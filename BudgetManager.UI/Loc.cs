using System.Globalization;

namespace BudgetManager.UI
{
    /// <summary>
    /// Tiny localization helper. English text is used directly as the lookup key, so
    /// English is the identity fallback and only Spanish needs a translation table.
    /// Use <see cref="T"/> for plain strings and <see cref="F"/> for format strings.
    /// </summary>
    internal static class Loc
    {
        /// <summary>Current language code: "en" or "es".</summary>
        public static string Language { get; private set; } = "en";

        public static void SetLanguage(string language)
        {
            Language = language == "es" ? "es" : "en";
            var culture = CultureInfo.GetCultureInfo(Language == "es" ? "es-MX" : "en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        /// <summary>Translate a string (returns the English key itself when in English or unmapped).</summary>
        public static string T(string english)
            => Language == "es" && Es.TryGetValue(english, out var s) ? s : english;

        /// <summary>Translate a format string and apply arguments.</summary>
        public static string F(string english, params object?[] args)
            => string.Format(T(english), args);

        private static readonly Dictionary<string, string> Es = new(StringComparer.Ordinal)
        {
            // App chrome
            ["Budget Manager"] = "Administrador de Presupuesto",
            ["Profile:"] = "Perfil:",
            ["Language:"] = "Idioma:",
            ["Manage Profiles"] = "Administrar perfiles",
            ["Open Data Folder"] = "Abrir carpeta de datos",
            ["Export…"] = "Exportar…",
            ["Import…"] = "Importar…",
            ["Refresh"] = "Actualizar",
            ["Dashboard"] = "Panel",
            ["Accounts"] = "Cuentas",
            ["Transactions"] = "Transacciones",
            ["Categories"] = "Categorías",
            ["Goals"] = "Metas",
            ["Graph"] = "Gráfica",
            ["About"] = "Acerca de",
            ["Version {0}"] = "Versión {0}",
            ["All rights reserved."] = "Todos los derechos reservados.",
            ["Contact"] = "Contacto",
            ["Acknowledgements"] = "Reconocimientos",
            ["Built with .NET and Windows Forms. Uses System.Text.Json for local storage, Microsoft.Extensions (Dependency Injection, Logging, Options), and Dapper with Microsoft.Data.SqlClient for the optional SQL persistence. Charts and the app icon are rendered with GDI+. This software is provided \"as is\", without warranty of any kind."]
                = "Desarrollada con .NET y Windows Forms. Usa System.Text.Json para el almacenamiento local, Microsoft.Extensions (Inyección de dependencias, Registro, Opciones) y Dapper con Microsoft.Data.SqlClient para la persistencia SQL opcional. Las gráficas y el ícono se dibujan con GDI+. Este software se proporciona \"tal cual\", sin garantía de ningún tipo.",
            ["Error"] = "Error",
            ["Recurring"] = "Recurrentes",

            // Data tools
            ["Export budget data"] = "Exportar datos de presupuesto",
            ["Import budget data"] = "Importar datos de presupuesto",
            ["Zip archive (*.zip)|*.zip"] = "Archivo Zip (*.zip)|*.zip",
            ["Exported {0} data file(s) to:\n{1}"] = "Se exportaron {0} archivo(s) de datos a:\n{1}",
            ["Export complete"] = "Exportación completa",
            ["Export failed"] = "Exportación fallida",
            ["Importing will overwrite existing data files with the contents of the backup. Continue?"]
                = "La importación sobrescribirá los archivos de datos existentes con el contenido del respaldo. ¿Continuar?",
            ["Confirm import"] = "Confirmar importación",
            ["Restored {0} data file(s)."] = "Se restauraron {0} archivo(s) de datos.",
            ["Import complete"] = "Importación completa",
            ["Import failed"] = "Importación fallida",

            // Common actions / words
            ["Add"] = "Agregar",
            ["Edit"] = "Editar",
            ["Delete"] = "Eliminar",
            ["Confirm"] = "Confirmar",
            ["Validation"] = "Validación",
            ["OK"] = "Aceptar",
            ["Cancel"] = "Cancelar",
            ["Close"] = "Cerrar",
            ["Yes"] = "Sí",
            ["No"] = "No",
            ["(none)"] = "(ninguno)",
            ["TOTAL"] = "TOTAL",
            ["Name is required."] = "El nombre es obligatorio.",

            // Selection prompts
            ["Select an account first."] = "Seleccione una cuenta primero.",
            ["Select a transaction first."] = "Seleccione una transacción primero.",
            ["Select a category first."] = "Seleccione una categoría primero.",
            ["Select a goal first."] = "Seleccione una meta primero.",
            ["Select a recurring item first."] = "Seleccione un elemento recurrente primero.",
            ["Select a profile first."] = "Seleccione un perfil primero.",
            ["Create an account first."] = "Cree una cuenta primero.",

            // Common column headers / field labels
            ["Name"] = "Nombre",
            ["Type"] = "Tipo",
            ["Amount"] = "Monto",
            ["Category"] = "Categoría",
            ["Account"] = "Cuenta",
            ["Date"] = "Fecha",
            ["Description"] = "Descripción",
            ["Currency"] = "Moneda",
            ["Balance"] = "Saldo",

            // Accounts
            ["Initial balance"] = "Saldo inicial",
            ["Archived"] = "Archivada",
            ["New account"] = "Nueva cuenta",
            ["Edit account"] = "Editar cuenta",
            ["Delete account '{0}'?"] = "¿Eliminar la cuenta '{0}'?",
            ["Earns interest"] = "Genera intereses",
            ["Annual rate %"] = "Tasa anual %",
            ["Interest frequency"] = "Frecuencia de interés",
            ["Next interest date"] = "Próxima fecha de interés",
            ["Interest"] = "Interés",
            ["Gained interest"] = "Interés ganado",

            // Transactions
            ["To (transfer)"] = "A (transferencia)",
            ["Filter:"] = "Filtro:",
            ["All accounts"] = "Todas las cuentas",
            ["Recurring transactions (auto-posted when due)"] = "Transacciones recurrentes (se registran al vencer)",
            ["Run due now"] = "Ejecutar vencidas",
            ["Enable/Disable"] = "Habilitar/Deshabilitar",
            ["Delete this transaction?"] = "¿Eliminar esta transacción?",
            ["{0} txns"] = "{0} trans.",
            ["Income {0}  ·  Expense {1}  ·  Net {2}"] = "Ingresos {0}  ·  Gastos {1}  ·  Neto {2}",
            ["Income {0}  ·  Expense {1}  ·  Transfers +{2}/-{3}  ·  Net {4}"]
                = "Ingresos {0}  ·  Gastos {1}  ·  Transferencias +{2}/-{3}  ·  Neto {4}",
            ["Enabled"] = "Habilitada",
            ["Disabled"] = "Deshabilitada",
            ["{0} item(s)"] = "{0} elemento(s)",
            ["Frequency"] = "Frecuencia",
            ["Next run"] = "Próxima",
            ["Posted {0} transaction(s)."] = "Se registraron {0} transacción(es).",
            ["Nothing was due."] = "No había vencidas.",

            // Transaction dialog
            ["New transaction"] = "Nueva transacción",
            ["Edit transaction"] = "Editar transacción",
            ["From account"] = "Cuenta origen",
            ["To account"] = "Cuenta destino",
            ["A source account is required."] = "Se requiere una cuenta de origen.",
            ["Amount must be greater than zero."] = "El monto debe ser mayor que cero.",
            ["A transfer needs a destination account."] = "Una transferencia necesita una cuenta de destino.",
            ["Source and destination must differ."] = "El origen y el destino deben ser diferentes.",

            // Recurring dialog
            ["New recurring transaction"] = "Nueva transacción recurrente",
            ["Edit recurring transaction"] = "Editar transacción recurrente",
            ["positive = income, negative = expense"] = "positivo = ingreso, negativo = gasto",
            ["Set a destination to make it a transfer to your own account."] = "Elija un destino para convertirla en una transferencia a su propia cuenta.",
            ["Next execution"] = "Próxima ejecución",
            ["Has end date"] = "Tiene fecha de fin",
            ["End date"] = "Fecha de fin",
            ["End date cannot be before the next run."] = "La fecha de fin no puede ser anterior a la próxima ejecución.",
            ["An account is required."] = "Se requiere una cuenta.",
            ["Amount cannot be zero."] = "El monto no puede ser cero.",
            ["Delete recurring item '{0}'?"] = "¿Eliminar el elemento recurrente '{0}'?",

            // Categories
            ["Parent"] = "Padre",
            ["Color"] = "Color",
            ["Icon"] = "Ícono",
            ["New category"] = "Nueva categoría",
            ["Edit category"] = "Editar categoría",
            ["Delete category '{0}'?"] = "¿Eliminar la categoría '{0}'?",

            // Goals
            ["Goal"] = "Meta",
            ["Target"] = "Objetivo",
            ["Current"] = "Actual",
            ["Progress %"] = "Progreso %",
            ["Due"] = "Vence",
            ["Target amount"] = "Monto objetivo",
            ["Current amount"] = "Monto actual",
            ["Has due date"] = "Tiene fecha límite",
            ["Due date"] = "Fecha límite",
            ["Add to goal"] = "Abonar a meta",
            ["New goal"] = "Nueva meta",
            ["Edit goal"] = "Editar meta",
            ["Delete goal '{0}'?"] = "¿Eliminar la meta '{0}'?",

            // Profiles
            ["Manage profiles"] = "Administrar perfiles",
            ["Profiles"] = "Perfiles",
            ["First names"] = "Nombres",
            ["Last names"] = "Apellidos",
            ["Email"] = "Correo",
            ["New profile"] = "Nuevo perfil",
            ["Edit profile"] = "Editar perfil",
            ["First names are required."] = "Los nombres son obligatorios.",
            ["Delete profile '{0} {1}'?"] = "¿Eliminar el perfil '{0} {1}'?",

            // Dashboard
            ["Net worth"] = "Patrimonio neto",
            ["Income (this month)"] = "Ingresos (este mes)",
            ["Expense (this month)"] = "Gastos (este mes)",
            ["Net (this month)"] = "Neto (este mes)",
            ["Account balances"] = "Saldos de cuentas",
            ["No goals yet."] = "Aún no hay metas.",

            // Safe to spend
            ["Safe to spend today"] = "Seguro para gastar hoy",
            ["Configure…"] = "Configurar…",
            ["Bills"] = "Cuentas por pagar",
            ["Safety buffer"] = "Colchón de seguridad",
            ["Goal reserve/day"] = "Reserva de metas/día",
            ["Reserve for goals"] = "Reservar para metas",
            ["Safe-to-spend settings"] = "Configuración de gasto seguro",
            ["{0} days to {1}"] = "{0} días hasta {1}",
            ["Daily allowance {0}  ·  Spent today {1}"] = "Asignación diaria {0}  ·  Gastado hoy {1}",
            ["Over-committed by {0}"] = "Comprometido de más por {0}",
            ["No upcoming income detected — using end of month."] = "No se detectan ingresos próximos — se usa fin de mes.",
            ["Mixed currencies — showing combined totals."] = "Monedas mixtas — se muestran totales combinados.",

            // Graph
            ["Start:"] = "Inicio:",
            ["◀ Week"] = "◀ Semana",
            ["Week ▶"] = "Semana ▶",
            ["◀ Month"] = "◀ Mes",
            ["Month ▶"] = "Mes ▶",
            ["Today"] = "Hoy",
            ["No data to project."] = "Sin datos para proyectar.",
            ["Projected balance"] = "Saldo proyectado",
            ["(uncategorized)"] = "(sin categoría)",
            ["▲ growing  +{0}"] = "▲ creciendo  +{0}",
            ["▼ shrinking  {0}"] = "▼ disminuyendo  {0}",
        };
    }
}
