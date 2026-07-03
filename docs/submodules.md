# Submodule Guide

Dreamine.MVVM.FullKit uses Git submodules because each package can be developed, versioned, and published independently while the FullKit repository records a known-good combination of all modules.

## Why So Many Submodules?

The project is intentionally split by responsibility:

- MVVM runtime modules: Core, Interfaces, ViewModels, Locators, WPF integration.
- UI modules: WPF, WPF Controls, WPF Themes, WPF Equipment, WinForms, MAUI, Blazor.
- Infrastructure modules: Logging, Threading, Communication, Database, IO, PLC.
- Composition/demo projects that show the modules working together.

The parent repository is the integration point. A parent commit tells users exactly which commit of each child repository was verified together.

## Cloning

Use recursive clone:

```powershell
git clone --recursive https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.FullKit.git
```

If the repository already exists:

```powershell
git submodule update --init --recursive
```

On Windows, enabling long paths can avoid checkout issues in deep sample folders:

```powershell
git config --global core.longpaths true
```

## Updating All Submodules

To sync submodule URLs and fetch the commits recorded by the parent repository:

```powershell
git submodule sync --recursive
git submodule update --init --recursive
```

## Editing A Submodule

Example for `Dreamine.UI.Wpf.Controls`:

```powershell
cd "20_SOURCES/100. Library/UI.Wpf.Controls"
git status
git add -u
git commit -m "Fix control docs"
git push origin main

cd "../../../.."
git add "20_SOURCES/100. Library/UI.Wpf.Controls"
git commit -m "Update UI.Wpf.Controls submodule"
git push origin main
```

The first commit changes the child repository. The second commit updates the FullKit pointer.

## Common Problems

### Empty or Missing Library Folders

Run:

```powershell
git submodule update --init --recursive
```

### Parent Repository Shows Modified Submodules

This usually means a child repository is checked out at a different commit than the parent expects. If you did not intend to update it:

```powershell
git submodule update --init --recursive
```

### Checkout Fails On Windows

Enable long paths and retry:

```powershell
git config --global core.longpaths true
git submodule update --init --recursive
```

## Release Tags

For releases, prefer tagging the FullKit parent repository after all child repositories have been pushed. The FullKit tag then represents a verified matrix of submodule commits.
