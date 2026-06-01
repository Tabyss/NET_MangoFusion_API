using MangoFusion_API.Data;
using MangoFusion_API.Models;
using MangoFusion_API.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace MangoFusion_API.Controllers
{
    [Route("api/MenuItem")]
    [ApiController]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ApiResponse _response;
        private readonly IWebHostEnvironment _env;
        public IActionResult Index()
        {
            return View();
        }
        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _response = new ApiResponse();
            _env = env;
        }

        [HttpGet]
        public IActionResult GetMenuItems()
        {
            try
            {
                var menuItems = _db.MenuItems.ToList();
                _response.Result = menuItems;
                _response.StatusCode = System.Net.HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                return StatusCode(500, _response);
            }
        }

        [HttpGet("{id:int}", Name = "GetMenuItem")]
        public IActionResult GetMenuItem(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Id");
                    _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                else
                {
                    MenuItem? menuItems = _db.MenuItems.FirstOrDefault(u => u.Id == id);
                    _response.Result = menuItems;
                    _response.StatusCode = System.Net.HttpStatusCode.OK;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                return StatusCode(500, _response);
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse>> CreateMenuItem([FromForm] MenuItemCreateDTO menuItemCreateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemCreateDTO == null)
                    {
                        _response.IsSuccess = false;
                        _response.ErrorMessages.Add("Invalid Data");
                        _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                        return BadRequest(_response);
                    }
                    else
                    {
                        string wwwRootPath = _env.WebRootPath;
                        string imageDirectory = Path.Combine(wwwRootPath, "images");
                        string filePath = Path.Combine(imageDirectory, menuItemCreateDTO.File.FileName);

                        if (!Directory.Exists(imageDirectory))
                        {
                            Directory.CreateDirectory(imageDirectory);
                        }
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await menuItemCreateDTO.File.CopyToAsync(fileStream);
                        }

                        var menuItem = new MenuItem
                        {
                            Name = menuItemCreateDTO.Name,
                            Description = menuItemCreateDTO.Description,
                            Category = menuItemCreateDTO.Category,
                            SpecialTag = menuItemCreateDTO.SpecialTag,
                            Price = menuItemCreateDTO.Price,
                            Image = @"\images\" + menuItemCreateDTO.File.FileName
                        };

                        _db.MenuItems.Add(menuItem);
                        await _db.SaveChangesAsync();
                        _response.IsSuccess = true;

                        _response.Result = menuItemCreateDTO;
                        _response.StatusCode = System.Net.HttpStatusCode.Created;
                        return CreatedAtRoute("GetMenuItem", new { id = menuItem.Id }, _response);
                    }
                }

                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Model state is invalid");
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                return StatusCode(500, _response);
            }
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse>> UpdateMenuItem(int id,[FromForm] MenuItemUpdateDTO menuItemUpdateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemUpdateDTO == null || menuItemUpdateDTO.Id != id)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        return BadRequest(_response);
                    }
                    else
                    {
                        MenuItem? menuItemFromDb = _db.MenuItems.FirstOrDefault(u => u.Id == id);

                        if (menuItemFromDb == null)
                        {
                            _response.IsSuccess = false;
                            _response.ErrorMessages.Add("Menu Item not found");
                            _response.StatusCode = HttpStatusCode.NotFound;
                            return NotFound(_response);
                        };

                        menuItemFromDb.Name = menuItemUpdateDTO.Name;
                        menuItemFromDb.Description = menuItemUpdateDTO.Description;
                        menuItemFromDb.Category = menuItemUpdateDTO.Category;
                        menuItemFromDb.SpecialTag = menuItemUpdateDTO.SpecialTag;
                        menuItemFromDb.Price = menuItemUpdateDTO.Price;

                        if (menuItemUpdateDTO.File != null && menuItemUpdateDTO.File.Length > 0)
                        {
                            var imagesPath = Path.Combine(_env.WebRootPath, "images");
                            if (!Directory.Exists(imagesPath))
                            {
                                Directory.CreateDirectory(imagesPath);
                            }

                            if (!string.IsNullOrEmpty(menuItemFromDb.Image))
                            {
                                var filePath_OldFile = Path.Combine(_env.WebRootPath, menuItemFromDb.Image.TrimStart('\\'));

                                if (System.IO.File.Exists(filePath_OldFile))
                                {
                                    System.IO.File.Delete(filePath_OldFile);
                                }
                            }

                            string fileName = Guid.NewGuid().ToString();
                            string extension = Path.GetExtension(menuItemUpdateDTO.File.FileName);
                            var filePath_NewFile = Path.Combine(imagesPath, fileName + extension);

                            using (var stream = new FileStream(filePath_NewFile, FileMode.Create))
                            {
                                await menuItemUpdateDTO.File.CopyToAsync(stream);
                            }

                            menuItemFromDb.Image = @"\images\" + fileName + extension;
                        }

                        _db.MenuItems.Update(menuItemFromDb);
                        await _db.SaveChangesAsync();

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.Result = menuItemFromDb;
                        return Ok(_response);

                    }
                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = [ex.ToString()];
            }

            return BadRequest(_response);
        }
        [HttpDelete]
        public async Task<ActionResult<ApiResponse>> DeleteMenuItem(int id)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (id == 0)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        return BadRequest(_response);
                    }

                    MenuItem? menuItemFromDb = await _db.MenuItems.FirstOrDefaultAsync(u => u.Id == id);

                    if (menuItemFromDb == null)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.NotFound;
                        return NotFound(_response);
                    }

                    var filePath_OldFile = Path.Combine(_env.WebRootPath, menuItemFromDb.Image);
                    if (System.IO.File.Exists(filePath_OldFile))
                    {
                        System.IO.File.Delete(filePath_OldFile);
                    }
                    _db.MenuItems.Remove(menuItemFromDb);
                    await _db.SaveChangesAsync();

                    _response.StatusCode = HttpStatusCode.NoContent;
                    return Ok(_response);

                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = [ex.ToString()];
            }

            return BadRequest(_response);
        }
    }
}
