class_name SkinLoader

const KEYS = ["basic", "dark", "light", "dark2", "light2", "background"]

const DEFAULT_SKIN = {
  # order is bottom / right / top / left
  "dark2":      ["00c8d9", "d80071", "8808cf", "b5e300"],
  "dark":       ["00d3e5", "e50078", "9208dd", "beed00"],
  "basic":      ["00ebff", "ff0085", "a209f6", "ccff00"],
  "light":      ["37efff", "ff2597", "ac25f6", "d8ff38"],
  "light2":     ["5cf1ff", "ff38a0", "b236f6", "dfff5c"],
  "background": ["8cf4ff", "ff87c6", "ba7add", "e8ff8c"],
}

const GOOGL_SKIN = {
  "dark2":      ["3597d9", "2e9148", "e3a52b", "c93c29"],
  "dark":       ["38a0e5", "319c4d", "f0ae2e", "d43f2b"],
  "basic":      ["3eb2ff", "37b057", "ffb831", "e2432e"],
  "light":      ["37efff", "49b566", "ffbf45", "e5513d"],
  "light2":     ["61c0ff", "51bd6f", "ffc454", "eb5a47"],
  "background": ["8cd1ff", "6ec784", "ffd78c", "f29285"],
}

static func load_skin(file_name):
  var load_file = File.new()
  if load_file.file_exists(file_name):
    load_file.open(file_name, File.READ)
    var data: Dictionary = parse_json(load_file.get_line())
    load_file.close()
    if data.has_all(KEYS):
      return data
  return null
