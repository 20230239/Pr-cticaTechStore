import sys, os
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.lib import colors
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image, PageBreak, KeepTogether)

DATOS = {
    "autores": "[COMPLETAR: nombres del equipo]",
    "curso": "[COMPLETAR: asignatura / docente / fecha]",
    "entorno": {
        "SO": "[COMPLETAR]", "CPU / RAM": "[COMPLETAR]", "Disco": "[COMPLETAR: SSD/HDD]",
        "SQL Server": "[COMPLETAR: version / edicion / local o Docker]",
        ".NET SDK": "8.0.x", "EF Core": "8.0.11", "Microsoft.Data.SqlClient": "5.2.2",
        "Repeticiones por medicion": "5 (+1 de calentamiento)",
    },
    "metricas": {
        "Lineas de codigo (scripts/contar_lineas.sh)": (None, None, None),
        "Archivos de codigo":                         (None, None, None),
        "Tiempo de desarrollo (horas)":               (None, None, None),
        "Errores encontrados (cantidad)":             (None, None, None),
        "Tiempo corrigiendo errores (min)":           (None, None, None),
        "Cambio BD GarantiaMeses: archivos tocados":  (None, None, None),
        "Cambio BD GarantiaMeses: lineas modificadas": (None, None, None),
        "Cambio BD GarantiaMeses: minutos":           (None, None, None),
    },
}

ss = getSampleStyleSheet()
BODY = ParagraphStyle("body", parent=ss["Normal"], fontName="Helvetica", fontSize=10, leading=14, spaceAfter=6)
H1 = ParagraphStyle("h1", parent=ss["Heading1"], fontSize=15, spaceBefore=12, spaceAfter=6, textColor=colors.HexColor("#1F3A5F"))
H2 = ParagraphStyle("h2", parent=ss["Heading2"], fontSize=11.5, spaceBefore=8, spaceAfter=4, textColor=colors.HexColor("#1F3A5F"))
TITLE = ParagraphStyle("title", parent=ss["Title"], fontSize=21, leading=26, textColor=colors.HexColor("#1F3A5F"))
SMALL = ParagraphStyle("small", parent=BODY, fontSize=8.5, leading=11, textColor=colors.HexColor("#444444"))
TODO = ParagraphStyle("todo", parent=BODY, backColor=colors.HexColor("#FFF3CD"), borderPadding=4, textColor=colors.HexColor("#6B4E00"))
BUL = ParagraphStyle("bul", parent=BODY, leftIndent=14, bulletIndent=2, spaceAfter=2)

def P(t, s=BODY): return Paragraph(t, s)
def bullets(items): return [Paragraph(i, BUL, bulletText="\u2022") for i in items]
def todo(t): return Paragraph("[COMPLETAR] " + t, TODO)

def tabla(data, widths=None, header=True, font=8.5):
    t = Table(data, colWidths=widths, repeatRows=1 if header else 0)
    st = [("FONTSIZE", (0, 0), (-1, -1), font), ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#999999")),
          ("VALIGN", (0, 0), (-1, -1), "MIDDLE"), ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F3F6FA")])]
    if header:
        st += [("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#1F3A5F")), ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
               ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold")]
    t.setStyle(TableStyle(st))
    return t

def cell(v): return "[completar]" if v is None else str(v)

df = None
csv = sys.argv[1] if len(sys.argv) > 1 else None
if csv and os.path.exists(csv):
    import pandas as pd
    df = pd.read_csv(csv)

def graficas(df, outdir):
    import matplotlib; matplotlib.use("Agg")
    import matplotlib.pyplot as plt, numpy as np
    os.makedirs(outdir, exist_ok=True)
    esc = list(dict.fromkeys(df["Escenario"]))
    techs = list(dict.fromkeys(df["Tecnologia"]))
    cols = {"ADO.NET": "#1F3A5F", "EF Core (tracking)": "#D9822B", "EF Core (AsNoTracking)": "#5BA55B"}
    paths = []
    for k in range(0, len(esc), 4):
        grupo = esc[k:k + 4]
        fig, axs = plt.subplots(2, 2, figsize=(7.4, 4.6)); axs = axs.flatten()
        for ax, e in zip(axs, grupo):
            d = df[df["Escenario"] == e]; vols = sorted(d["Volumen"].unique()); w = 0.8 / max(1, len(techs))
            for i, t in enumerate(techs):
                dt = d[d["Tecnologia"] == t].set_index("Volumen")
                if dt.empty: continue
                y = [dt["MediaMs"].get(v, np.nan) for v in vols]
                ax.bar(np.arange(len(vols)) + i * w, y, w, label=t, color=cols.get(t))
            ax.set_xticks(np.arange(len(vols)) + w * (len(techs) - 1) / 2); ax.set_xticklabels([f"{v:,}" for v in vols], fontsize=7)
            ax.set_yscale("log"); ax.set_ylabel("ms (escala log)", fontsize=7); ax.tick_params(axis="y", labelsize=7)
            ax.set_title(e, fontsize=7.5, wrap=True)
        for ax in axs[len(grupo):]: ax.axis("off")
        h, l = axs[0].get_legend_handles_labels(); fig.legend(h, l, loc="lower center", ncol=3, fontsize=8)
        fig.tight_layout(rect=(0, 0.05, 1, 1))
        p = os.path.join(outdir, f"grafica_{k // 4 + 1}.png"); fig.savefig(p, dpi=170); plt.close(fig); paths.append(p)
    return paths

here = os.path.dirname(os.path.abspath(__file__))
out = os.path.join(here, "Informe_TechStore.pdf")
doc = SimpleDocTemplate(out, pagesize=letter, leftMargin=0.9 * inch, rightMargin=0.9 * inch, topMargin=0.8 * inch, bottomMargin=0.8 * inch,
                        title="ADO.NET vs Entity Framework Core - TechStore", author="Equipo TechStore")
def footer(c, d):
    c.saveState(); c.setFont("Helvetica", 8); c.setFillColor(colors.grey)
    c.drawCentredString(letter[0] / 2, 0.5 * inch, f"ADO.NET vs EF Core - TechStore  |  Pagina {d.page}"); c.restoreState()

S = []
S += [Spacer(1, 40), P("Comparativa practica de ADO.NET y Entity Framework Core", TITLE),
      P("Caso de estudio: sistema de inventario y ventas TechStore", ParagraphStyle("sub", parent=BODY, alignment=1, fontSize=12)),
      Spacer(1, 10), P(DATOS["autores"], ParagraphStyle("c", parent=BODY, alignment=1)),
      P(DATOS["curso"], ParagraphStyle("c2", parent=BODY, alignment=1)), Spacer(1, 18)]
S += [P("Resumen", H1),
      P("TechStore necesita un modulo de inventario y ventas y quiere decidir con datos que tecnologia de acceso a datos adoptar. "
        "Se implemento el mismo modulo dos veces sobre la misma base de datos SQL Server (TechStoreDB, con 100,000 productos), "
        "una con ADO.NET (Microsoft.Data.SqlClient) y otra con Entity Framework Core 8 (Code First con migraciones; modelo Database First generado con scaffold), "
        "ambas cumpliendo las mismas interfaces. Se midieron seis escenarios con volumenes de 1,000 a 100,000 registros, y se registraron metricas de desarrollo: "
        "lineas de codigo, tiempo, errores y esfuerzo ante un cambio de esquema."),
      todo("Resumen de 2-3 lineas con el resultado principal y la recomendacion final (escribelo al terminar, con tus numeros).")]

S += [P("1. Introduccion y objetivo", H1),
      P("ADO.NET es la capa base de .NET para hablar con bases de datos relacionales: el desarrollador escribe SQL, controla conexiones, parametros y lectura de resultados. "
        "Entity Framework Core es un ORM que traduce consultas LINQ a SQL, mapea filas a objetos, rastrea cambios y gestiona migraciones. "
        "El objetivo es comparar ambas en rendimiento (tiempo y memoria) y en esfuerzo de desarrollo y mantenimiento, y recomendar una para TechStore a partir de los datos propios."),
      P("Pregunta de investigacion: <i>cuanto rendimiento se sacrifica (o se gana) al usar EF Core frente a ADO.NET, y que ahorro de desarrollo se obtiene a cambio?</i>")]

S += [P("2. Entorno experimental", H1)]
S += [tabla([["Elemento", "Valor"]] + [[k, v] for k, v in DATOS["entorno"].items()], [2.3 * inch, 4.3 * inch])]
S += [Spacer(1, 6), P("Las pruebas se ejecutaron en modo Release, con SQL Server en la misma maquina, la misma cadena de conexion para ambas tecnologias y la cache del plan de ejecucion calentada con una corrida previa no registrada.", SMALL)]

S += [P("3. Diseno de la base de datos y de la solucion", H1),
      P("<b>Base de datos.</b> TechStoreDB contiene Categorias, Productos, Clientes, Ventas y DetalleVentas, con claves primarias, claves foraneas, restricciones UNIQUE (SKU, Email, Nombre de categoria) "
        "y CHECK (Precio > 0, Stock >= 0, Cantidad > 0, Total >= 0). Se cargaron 10 categorias, 10,000 clientes y 100,000 productos con un unico INSERT...SELECT. "
        "Hay indices en Productos(CategoriaId) y Productos(Nombre)."),
      P("<b>Solucion.</b> Seis proyectos: <i>Core</i> (entidades, interfaces IProductoRepository, IClienteRepository, IVentaRepository), <i>AdoNet</i>, <i>EFCore</i> (DbContext con Fluent API, migraciones y repositorios), "
        "<i>EFCore.DbFirst</i> (modelo generado con scaffold), <i>Benchmarks</i> y <i>Demo</i>. Compartir las interfaces garantiza que la comparacion sea justa."),
      P("<b>Decisiones de implementacion relevantes:</b>")]
S += bullets([
    "ADO.NET: todas las consultas son parametrizadas (SqlParameter con tipo y tamano); carga por lote con SqlBulkCopy; busqueda con LIKE 'prefijo%' que aprovecha el indice.",
    "Venta transaccional (ambas): se agrupan y ordenan las lineas por producto (evita deadlocks), se descuenta stock con <i>UPDATE ... WHERE Stock >= @cant</i> (atomico, sin condicion de carrera) y se inserta cabecera y detalle; ante cualquier error, Rollback.",
    "EF Core: se uso transaccion explicita (BeginTransactionAsync) porque el descuento de stock se hace con ExecuteUpdate, que no participa en SaveChanges. La cabecera y el detalle se guardan con un solo SaveChanges.",
    "EF Core se mide en dos configuraciones: con seguimiento de cambios (tracking) y con AsNoTracking en lecturas.",
    "Code First: el DbContext reproduce exactamente el esquema del script (nombres, tipos, indices, checks); la migracion 'Inicial' se aplica a una BD vacia y se registra como linea base en TechStoreDB.",
    "Database First: <i>dotnet ef dbcontext scaffold</i> genera el modelo desde TechStoreDB; se compara con el modelo escrito a mano."])

S += [P("4. Metodologia de pruebas", H1),
      P("Cada escenario se ejecuta una vez como calentamiento (no registrado) y despues 5 veces; se reporta la media, la desviacion estandar y la memoria asignada (GC.GetTotalAllocatedBytes). "
        "La preparacion de datos y la limpieza de cada repeticion no se cronometran. Cada operacion abre su propia conexion (con pool de conexiones) y, en EF Core, su propio DbContext.")]
S += [tabla([["#", "Escenario", "Volumenes", "Que mide"],
             ["1a", "Insertar individual", "1,000 / 10,000", "Costo por INSERT (un viaje al servidor por fila)"],
             ["1b", "Insertar en lote", "1,000 a 100,000", "SqlBulkCopy vs AddRange+SaveChanges"],
             ["2", "Consulta por ID", "1,000 / 10,000 / 50,000", "Sobrecosto por consulta puntual y materializacion"],
             ["3a", "Busqueda filtrada", "100 / 1,000 / 5,000 consultas", "LIKE por prefijo + paginacion con indice"],
             ["3b", "Paginacion", "1,000 a 100,000 filas", "OFFSET/FETCH en paginas de 500"],
             ["4", "JOIN producto-categoria", "1,000 a 100,000 filas", "Materializacion y seguimiento de cambios"],
             ["5a", "Actualizacion masiva (set-based)", "1,000 a 100,000 filas", "UPDATE vs ExecuteUpdate"],
             ["5b", "Actualizacion por entidades", "1,000 a 100,000 filas", "Cargar + modificar + SaveChanges (solo EF)"],
             ["6", "Venta transaccional", "50 / 500 ventas", "Costo de la transaccion de 3 lineas"]],
            [0.35 * inch, 1.9 * inch, 1.7 * inch, 2.65 * inch], font=8)]

S += [PageBreak(), P("5. Resultados de rendimiento", H1)]
if df is not None:
    S += [P("Las graficas muestran el tiempo medio en milisegundos (escala logaritmica; menor es mejor). Todos los datos provienen del archivo %s generado por TechStore.Benchmarks." % os.path.basename(csv))]
    for p in graficas(df, os.path.join(here, "graficas")):
        S += [Image(p, width=6.0 * inch, height=6.0 * inch * 4.6 / 7.4), Spacer(1, 6)]
    S += [P("Tabla de resultados (tiempo medio en ms; entre parentesis, desviacion estandar)", H2)]
    techs = list(dict.fromkeys(df["Tecnologia"]))
    rows = [["Escenario", "N"] + techs]
    for (e, v), g in df.groupby(["Escenario", "Volumen"], sort=False):
        fila = [e[:44], f"{v:,}"]
        for t in techs:
            x = g[g["Tecnologia"] == t]
            fila.append("-" if x.empty else f"{x['MediaMs'].iloc[0]:,.1f} ({x['DesvMs'].iloc[0]:,.1f})")
        rows.append(fila)
    S += [tabla(rows, [2.5 * inch, 0.6 * inch] + [1.15 * inch] * len(techs), font=7)]
    S += [Spacer(1, 6), P("Memoria asignada promedio (MB)", H2)]
    rows = [["Escenario", "N"] + techs]
    for (e, v), g in df.groupby(["Escenario", "Volumen"], sort=False):
        fila = [e[:44], f"{v:,}"]
        for t in techs:
            x = g[g["Tecnologia"] == t]
            fila.append("-" if x.empty else f"{x['KbAsignados'].iloc[0] / 1024:,.1f}")
        rows.append(fila)
    S += [tabla(rows, [2.5 * inch, 0.6 * inch] + [1.15 * inch] * len(techs), font=7)]
else:
    S += [todo("Ejecuta los benchmarks (README, seccion 5) y vuelve a generar este PDF pasando el CSV: "
               "<font face='Courier'>python3 Informe/generar_informe.py resultados.csv</font>. "
               "Se insertaran automaticamente las graficas, las tablas de tiempo y memoria y el cuadro de razones EF/ADO.NET.")]

S += [P("6. Metricas de desarrollo", H1),
      P("Datos de la bitacora del equipo (scripts/bitacora.csv y scripts/errores.csv) y de scripts/contar_lineas.sh. "
        "La prueba de cambio consistio en agregar la columna <i>GarantiaMeses</i> a Productos (CAMBIO_BD/01_agregar_garantia.sql) y propagarla a cada implementacion.")]
m = DATOS["metricas"]
S += [tabla([["Metrica", "ADO.NET", "EF Core Code First", "EF Core Database First"]] +
            [[k, cell(a), cell(b), cell(c)] for k, (a, b, c) in m.items()],
            [2.9 * inch, 1.0 * inch, 1.5 * inch, 1.5 * inch], font=8)]
S += [Spacer(1, 6), P("Errores mas relevantes encontrados", H2), todo("Describe 2 o 3 errores reales (causa, como se detecto, tiempo de correccion) en cada tecnologia. Ejemplos tipicos a vigilar: tipo/escala de parametros decimales, indices de columnas en el Map de ADO.NET, traduccion de LINQ a SQL, migracion que choca con tablas ya existentes.")]

S += [P("7. Analisis", H1)]
if df is not None:
    S += [P("Razon de tiempo respecto a ADO.NET en el mayor volumen de cada escenario (valores > 1 indican que la variante es mas lenta que ADO.NET):")]
    rows = [["Escenario (N maximo)", "EF tracking / ADO", "EF NoTracking / ADO"]]
    for e, g in df.groupby("Escenario", sort=False):
        vmax = g["Volumen"].max(); gv = g[g["Volumen"] == vmax].set_index("Tecnologia")["MediaMs"]
        if "ADO.NET" in gv.index:
            r1 = f"{gv['EF Core (tracking)'] / gv['ADO.NET']:.2f}x" if "EF Core (tracking)" in gv.index else "-"
            r2 = f"{gv['EF Core (AsNoTracking)'] / gv['ADO.NET']:.2f}x" if "EF Core (AsNoTracking)" in gv.index else "-"
            rows.append([f"{e[:52]} ({vmax:,})", r1, r2])
        else:
            rows.append([f"{e[:52]} ({vmax:,})", "sin equivalente en ADO", "-"])
    S += [tabla(rows, [3.6 * inch, 1.5 * inch, 1.5 * inch], font=8), Spacer(1, 6)]
S += [P("<b>Hipotesis que los datos deben confirmar o refutar</b> (basadas en el funcionamiento de cada tecnologia; no son resultados):", BODY)]
S += bullets([
    "Lecturas grandes (escenarios 3b y 4): EF con tracking asigna mas memoria y tarda mas porque crea un 'entry' de seguimiento por entidad; AsNoTracking deberia acercarse a ADO.NET.",
    "Consultas puntuales (escenario 2): la diferencia por consulta es pequena y se debe al pipeline de EF (traduccion y materializacion); depende del cache de consultas compiladas.",
    "Insercion en lote (1b): SqlBulkCopy suele superar con claridad a AddRange+SaveChanges, que envia sentencias agrupadas pero con costo por entidad.",
    "Actualizacion masiva (5a/5b): ExecuteUpdate deberia igualar al UPDATE de ADO.NET, mientras que cargar y guardar entidades sera mucho mas lento y consumira mas memoria.",
    "Venta transaccional (6): dominada por viajes de ida y vuelta a la BD; se espera diferencia moderada."])
S += [todo("Para cada hipotesis, escribe si se cumplio, con el numero que lo respalda (ej. 'AsNoTracking redujo el tiempo del JOIN de X ms a Y ms con 100,000 filas'), y explica la causa."),
      P("Productividad y mantenimiento", H2),
      todo("Compara lineas de codigo, tiempo y esfuerzo ante el cambio de esquema (seccion 6): cuantos archivos hubo que tocar en ADO.NET frente a la migracion o re-scaffold en EF Core. "
           "Menciona el riesgo de errores en tiempo de ejecucion (SQL como texto) frente a la verificacion en compilacion de LINQ, y la comparacion entre Code First y Database First (cuando conviene cada uno).")]

S += [P("8. Conclusiones y recomendacion para TechStore", H1),
      todo("Escribe la recomendacion apoyada en TUS numeros. Estructura sugerida: (1) en que escenarios ADO.NET fue claramente mejor y por cuanto; (2) en cuales la diferencia fue irrelevante; "
           "(3) cuanto esfuerzo de desarrollo ahorro EF Core; (4) recomendacion final, incluida la opcion hibrida (EF Core para CRUD y reportes con AsNoTracking, ADO.NET o SQL directo para cargas masivas criticas); "
           "(5) limitaciones del estudio: una sola maquina, SQL Server local, datos sinteticos, sin concurrencia, un equipo con la misma experiencia en ambas tecnologias.")]

S += [P("9. Referencias", H1)]
refs = [
    "Microsoft. Entity Framework Core documentation. https://learn.microsoft.com/ef/core/",
    "Microsoft. ADO.NET overview. https://learn.microsoft.com/dotnet/framework/data/adonet/ado-net-overview",
    "Microsoft. Efficient querying and performance in EF Core. https://learn.microsoft.com/ef/core/performance/",
    "Microsoft. Tracking vs. no-tracking queries. https://learn.microsoft.com/ef/core/querying/tracking",
    "Microsoft. Migrations overview (EF Core). https://learn.microsoft.com/ef/core/managing-schemas/migrations/",
    "Microsoft. Scaffolding (reverse engineering). https://learn.microsoft.com/ef/core/managing-schemas/scaffolding/",
    "Microsoft. ExecuteUpdate and ExecuteDelete. https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete",
    "Microsoft. Bulk copy operations in SQL Server (SqlBulkCopy). https://learn.microsoft.com/sql/connect/ado-net/sql/bulk-copy-operations-sql-server",
    "Microsoft. Transactions in EF Core. https://learn.microsoft.com/ef/core/saving/transactions",
]
S += [P(f"[{i}] {r}", SMALL) for i, r in enumerate(refs, 1)]
S += [P("Anade las fuentes adicionales que uses (articulos, libros, benchmarks de terceros).", SMALL)]

doc.build(S, onFirstPage=footer, onLaterPages=footer)
print("Generado:", out)
