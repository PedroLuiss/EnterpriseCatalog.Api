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