# 🛒 Enterprise Catalog API (.NET RESTful API)

API RESTful profesional desarrollada en **.NET** orientada a la gestión segura de un catálogo comercial. El sistema implementa autenticación basada en **JWT (JSON Web Tokens)**, relaciones relacionales complejas entre **Categorías y Productos**, y **manejo/subida de archivos físicos (fotografías)** al servidor.

---

## 🛠️ Tecnologías y Características Técnicas

* **Backend:** C#, .NET (ASP.NET Core Web API).
* **Base de Datos:** SQL Server con **Entity Framework Core (Code First)**.
* **Seguridad & Autenticación:** 
  * Autenticación por **JWT Bearer**.
  * Encriptación de credenciales con **BCrypt**.
* **Gestión de Archivos:** Subida de imágenes multimedia (`Multipart/Form-Data`) almacenadas localmente y servidas mediante archivos estáticos (`StaticFiles`).
* **Documentación:** Interfaz interactiva integrada con **Swagger / OpenAPI**.

---

## 📦 Módulo de productos

El catálogo maneja productos generales y permite extender sus datos sin cambiar el esquema por cada categoría:

* **Datos comerciales:** SKU, código de barras, marca, fabricante, modelo y condición.
* **Precios e inventario:** precio de venta, costo, precio comparativo, moneda y existencias.
* **Logística:** peso y dimensiones con sus unidades.
* **Datos flexibles:** `AttributesJson` acepta propiedades específicas de cualquier categoría; `TagsJson` almacena etiquetas.
* **Variantes:** opciones como talla, color o capacidad; cada variante tiene SKU, código de barras, existencias y precio opcional. Si el precio de la variante es `null`, se usa el precio base del producto. El inventario de cada variante es independiente del campo `Stock` del producto.
* **Estado e imagen:** `IsActive` permite ocultar productos sin borrarlos; las imágenes JPG, PNG, GIF y WEBP se limitan a 5 MB.

Todos los endpoints requieren autenticación JWT:

| Método | Ruta | Acción |
|---|---|---|
| GET | `/api/products?page=1&pageSize=20&search=camisa&categoryId=1` | Lista paginada, con búsqueda y filtro por categoría. Por defecto omite productos inactivos. |
| GET | `/api/products/{id}` | Obtiene un producto con categoría, atributos y variantes. |
| POST | `/api/products` | Crea el producto y sus variantes. Recibe `multipart/form-data`. |
| PUT | `/api/products/{id}` | Reemplaza los datos principales del formulario y, si se envía, sincroniza variantes por SKU. Envía `Price`, `Stock` y `CategoryId`; los campos opcionales omitidos quedan vacíos o toman su valor predeterminado. |
| DELETE | `/api/products/{id}` | Desactiva el producto y sus variantes; no elimina sus datos. |

En POST y PUT, los campos `AttributesJson`, `TagsJson` y `VariantsJson` se envían como texto JSON dentro del formulario. Ejemplos:

```text
AttributesJson = {"material":"algodón","waterproof":true}
TagsJson = ["ropa","verano"]
VariantsJson = [{"sku":"CAM-ROJ-M","name":"Rojo / M","options":{"color":"rojo","talla":"M"},"price":null,"stock":12,"isActive":true}]
```

`Price`, `Stock` y `CategoryId` son obligatorios; el resto de datos adicionales se puede omitir. Si no se especifica `CurrencyCode`, se usa `USD`. En PUT, omitir `Image` conserva la imagen actual; `RemoveImage=true` la quita. Omitir `VariantsJson` conserva las variantes actuales; enviar `[]` elimina todas las variantes del producto. Para crear o editar estos campos, aplica las migraciones pendientes:

```powershell
dotnet ef database update
```

---

## 📥 Cómo Instalar y Correr el Proyecto Localmente

Sigue estos pasos detallados para configurar y poner en marcha la API en tu computadora:

### Prerrequisitos previos:
Asegúrate de tener instalado en tu equipo:
* **.NET SDK** (versión compatible con tu entorno, ej. .NET 8 o superior).
* **SQL Server** (puede ser SQL Server Express, Developer o LocalDB) junto con SQL Server Management Studio (SSMS).
* **Git** para clonar el repositorio.

---

### Paso 1: Clonar el repositorio
Abre tu terminal (CMD, PowerShell o Bash) y ejecuta el siguiente comando para clonar el proyecto en tu máquina:
```bash
git clone https://github.com/PedroLuiss/EnterpriseCatalog.Api.git
cd EnterpriseCatalog.Api

---

## 📐 Patrones de Diseño y Arquitectura

Este proyecto sigue una arquitectura limpia basada en **Separación de Responsabilidades (SoC)** y principios corporativos:
1. **Controladores delgados (Thin Controllers):** Exponen únicamente los endpoints HTTP necesarios delegando la lógica de seguridad y persistencia.
2. **Inyección de Dependencias (DI):** Servicios desacoplados (`TokenService`, `AppDbContext`) inyectados nativamente en el contenedor de .NET.
3. **Modelado Relacional (One-to-Many):** Relación estricta a nivel de base de datos entre `Category` y `Product`.

---

## 🚀 Guía de Instalación y Ejecución Local

Sigue estos pasos para clonar y poner en marcha la API en tu entorno de desarrollo:

### 1. Clonar el repositorio
```bash
git clone https://github.com/PedroLuiss/EnterpriseCatalog.Api.git
cd EnterpriseCatalog.Api