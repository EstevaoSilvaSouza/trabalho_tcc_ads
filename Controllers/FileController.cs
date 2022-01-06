using Gerenciador.Context;
using GERENCIADOR_TESTE_TEMPLANTE.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using X.PagedList;
using Microsoft.Extensions.FileProviders;
using Gerenciador.Models;
using Microsoft.AspNetCore.Identity;

namespace Gerenciador.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly Contexto _context;

        public FileController(Contexto context, UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            var recebedados = new FileUploadViewModel();
            recebedados.FilesOnDatabase = await _context.FilesOnDatabase.Where(l => l.status == "Pendente").Include(i => i.applicationUser).Include(b => b.categoria).Include(c => c.TipoTrabalho).ToListAsync();
            return View(recebedados);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador, Aluno, Professor")]
        public async Task<IActionResult> Arquivos()
        {
            var recebedados = new CateTipoFile();
            recebedados.categoria = await _context.categoria.ToListAsync();
            recebedados.tipoTrabalho= await _context.tipoTrabalho.ToListAsync();
            recebedados.professor = await _context.professor.ToListAsync();
            var usuario = userManager.Users.ToList();
            var categoria = await _context.categoria.ToListAsync();
            var TipoTraba = await _context.tipoTrabalho.ToListAsync();
            var professor = await _context.professor.ToListAsync();

            categoria.Insert(0, new Categoria
            {
                Id = 0,
                Nome = "Selecione uma categoria"
            });

            TipoTraba.Insert(0, new TipoTrabalho()
            {
                Id = 0,
                Nome = "Tipo de Trabalho"
            });

            professor.Insert(0, new Professor()
            {
                Id = 0,
                Nome = "Selecione o Orientador"
            });

            ViewBag.Categoria = categoria;
            ViewBag.TipoTrabalho = TipoTraba;
            ViewBag.Professor = professor;

            recebedados.FileOnDatabaseModel = await _context.FilesOnDatabase.Where(a => a.UsuarioId==User.Identity.Name).Include(i => i.applicationUser).Include(b => b.categoria).Include(c => c.TipoTrabalho).ToListAsync();
            return View(recebedados);
        }


        [Authorize(Roles = "Administrador, Aluno, Professor")]
        [HttpPost]
        public async Task<IActionResult> Arquivos(List<IFormFile> files, string descricao, CateTipoFile cateTipoFile,int ano, string autor, string titulo, string palavrachave)
        {
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var verifica = fileName;
                if (verifica.Contains("."))
                {
                    TempData["Message1"] = "Arquivo invalido!!, use um arquivo .pdf";
                    break;
                }
                if (extension != ".pdf")
                {
                    TempData["Message1"] = "Arquivo invalido!!, use um arquivo .pdf";
                    break;
                }
                else
                {
                    var fileModel = new FileOnDatabaseModel
                    {
                        DataCriacao = DateTime.UtcNow,
                        TipoArquivo = file.ContentType,
                        Estensao = extension,
                        NomeArquivo = fileName,
                        Descricao = descricao,
                        Autor = autor,
                        Titulo = titulo,
                        Cidade = "Ribeirão Preto",
                        PalavraChave = palavrachave,
                        AnoPubli = ano,
                        UsuarioId = User.Identity.Name,
                        status = "Pendente",
                        CategoriaId = cateTipoFile.FileOnDatabaseModelok.CategoriaId,
                        ProfessorId = cateTipoFile.FileOnDatabaseModelok.ProfessorId,
                        TipoTrabalhoId = cateTipoFile.FileOnDatabaseModelok.TipoTrabalhoId
                    };
                    using (var dataStream = new MemoryStream())
                    {
                        await file.CopyToAsync(dataStream);
                        fileModel.Data = dataStream.ToArray();
                    }
                    _context.FilesOnDatabase.Add(fileModel);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Arquivo Enviado com Sucesso!! aguarde para ser aprovado!!";
                }
            }
            return RedirectToAction("Arquivos");
        }

        public async Task<IActionResult> DownloadFileFromDatabase(int id)
        {
            var file = await _context.FilesOnDatabase.FindAsync(id);
            if (file == null) return null;
            return File(file.Data, file.TipoArquivo, file.NomeArquivo + file.Estensao);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteFileFromDatabase(int id)
        {

            var file = await _context.FilesOnDatabase.FindAsync(id);
            if (file == null) return null;
            _context.FilesOnDatabase.Remove(file);
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Removed {file.NomeArquivo + file.Estensao} Arquivo Deleteado com sucesso.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DetailsFileFromDatabase(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }
            var arquivo = await _context.FilesOnDatabase.FindAsync(id);

            if (arquivo == null)
            {
                return RedirectToAction("Index");
            }
            var categoriaa = _context.categoria.SingleOrDefault(a => arquivo.CategoriaId == a.Id);
            var professor = _context.professor.SingleOrDefault(a => arquivo.ProfessorId == a.Id);
            var tipotrabalho = _context.tipoTrabalho.SingleOrDefault(a => arquivo.TipoTrabalhoId == a.Id);
           

            var t = new FileModel()
            {
                Id = arquivo.Id,
                Autor = arquivo.Autor,
                Titulo = arquivo.Titulo,
                Cidade = arquivo.Cidade,
                PalavraChave = arquivo.PalavraChave,
                AnoPubli = arquivo.AnoPubli,
                NomeArquivo = arquivo.NomeArquivo,
                DataCriacao = arquivo.DataCriacao,
                TipoArquivo = arquivo.TipoArquivo,
                Estensao = arquivo.Estensao,
                Descricao = arquivo.Descricao,
                categoria =categoriaa,
                professor = professor,
                TipoTrabalho = tipotrabalho,
            };
            return View(t); 
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Acceptcontent(int id)
        {

            var file = await _context.FilesOnDatabase.FindAsync(id);
            file.status = "aprovado";

            _context.Entry(file).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Aprovado {file.NomeArquivo + file.Estensao} com sucesso.";
            return RedirectToAction("Index");
        }
    }
}
