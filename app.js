(() => {
  "use strict";

  const TWO_PI = Math.PI * 2;
  const DEG = Math.PI / 180;

  const PRESETS = {
    broadleaf: {
      style: "broadleaf",
      seed: 42017,
      height: 8.4,
      trunkRadius: 0.28,
      trunkTaper: 0.74,
      trunkCurve: 0.38,
      branchLevels: 7,
      branchFrequency: 0.78,
      branchDensity: 0.58,
      branchAngle: 48,
      branchLength: 0.74,
      crownWidth: 0.86,
      leafModule: "oval",
      leafDensity: 0.72,
      leafSize: 0.38,
      flowerModule: "none",
      flowerDensity: 0.15,
      flowerSize: 0.2,
      barkColor: "#8a6247",
      leafColor: "#4e8a45",
      leafAccentColor: "#79a64b",
      flowerColor: "#d95783",
      wireframe: false
    },
    conifer: {
      style: "conifer",
      seed: 61723,
      height: 10.2,
      trunkRadius: 0.23,
      trunkTaper: 0.82,
      trunkCurve: 0.16,
      branchLevels: 12,
      branchFrequency: 0.86,
      branchDensity: 0.78,
      branchAngle: 70,
      branchLength: 0.68,
      crownWidth: 0.82,
      leafModule: "needle",
      leafDensity: 0.82,
      leafSize: 0.24,
      flowerModule: "cone",
      flowerDensity: 0.28,
      flowerSize: 0.18,
      barkColor: "#735b45",
      leafColor: "#2f7350",
      leafAccentColor: "#4d8c63",
      flowerColor: "#9b6a42",
      wireframe: false
    },
    shrub: {
      style: "shrub",
      seed: 99551,
      height: 4.1,
      trunkRadius: 0.13,
      trunkTaper: 0.64,
      trunkCurve: 0.54,
      branchLevels: 6,
      branchFrequency: 0.9,
      branchDensity: 0.88,
      branchAngle: 57,
      branchLength: 0.82,
      crownWidth: 0.94,
      leafModule: "lobed",
      leafDensity: 0.86,
      leafSize: 0.25,
      flowerModule: "blossom",
      flowerDensity: 0.5,
      flowerSize: 0.17,
      barkColor: "#816043",
      leafColor: "#5f9448",
      leafAccentColor: "#91a84f",
      flowerColor: "#d95783",
      wireframe: false
    }
  };

  const canvas = document.querySelector("#plantCanvas");
  const ctx = canvas.getContext("2d", { alpha: false });
  const meshStats = document.querySelector("#meshStats");
  const inputs = [...document.querySelectorAll("[data-param]")];

  const state = {
    config: { ...PRESETS.broadleaf },
    mesh: null,
    activePreset: "broadleaf",
    camera: {
      yaw: -0.58,
      pitch: -0.24,
      zoom: 1
    },
    pointer: {
      active: false,
      x: 0,
      y: 0
    },
    generateFrame: 0,
    renderFrame: 0
  };

  class Mesh {
    constructor(materials) {
      this.vertices = [];
      this.faces = [];
      this.materials = materials;
      this.meta = {
        branches: 0,
        leaves: 0,
        flowers: 0,
        junctions: 0
      };
      this.bounds = {
        min: vec(0, 0, 0),
        max: vec(0, 0, 0),
        center: vec(0, 0, 0),
        radius: 1
      };
    }

    addVertex(position) {
      this.vertices.push(position);
      return this.vertices.length - 1;
    }

    addFace(a, b, c, material) {
      if (a === b || b === c || c === a) {
        return;
      }
      this.faces.push({ a, b, c, material });
    }
  }

  const partModules = {
    leaf: {
      oval(mesh, position, direction, scale, config, rng) {
        const points = [];
        for (let i = 0; i < 10; i += 1) {
          const angle = (i / 10) * TWO_PI;
          points.push({
            x: Math.cos(angle) * 0.48,
            y: Math.sin(angle) * 0.78
          });
        }
        addPlanarPart(mesh, position, direction, scale, points, pickLeafMaterial(rng));
      },
      lobed(mesh, position, direction, scale, config, rng) {
        const points = [];
        for (let i = 0; i < 12; i += 1) {
          const angle = (i / 12) * TWO_PI;
          const radius = i % 2 === 0 ? 0.78 : 0.38;
          points.push({
            x: Math.cos(angle) * radius,
            y: Math.sin(angle) * radius
          });
        }
        addPlanarPart(mesh, position, direction, scale * 0.92, points, pickLeafMaterial(rng));
      },
      needle(mesh, position, direction, scale, config, rng) {
        const count = 4 + Math.round(config.leafDensity * 5);
        const axis = normalize(direction);
        for (let i = 0; i < count; i += 1) {
          const spread = normalize(add(scaleVec(axis, 0.55), randomConeVector(rng, 0.9)));
          const base = add(position, scaleVec(randomConeVector(rng, 1), scale * 0.12));
          const tip = add(base, scaleVec(spread, scale * randRange(rng, 0.9, 1.45)));
          const side = safeSideVector(spread, scale * 0.045);
          const a = mesh.addVertex(add(base, side));
          const b = mesh.addVertex(sub(base, side));
          const c = mesh.addVertex(tip);
          mesh.addFace(a, b, c, "leaf");
          mesh.meta.leaves += 1;
        }
      },
      blade(mesh, position, direction, scale, config, rng) {
        const points = [
          { x: 0, y: -0.9 },
          { x: 0.18, y: -0.18 },
          { x: 0.1, y: 0.82 },
          { x: 0, y: 1.05 },
          { x: -0.1, y: 0.82 },
          { x: -0.18, y: -0.18 }
        ];
        addPlanarPart(mesh, position, direction, scale, points, pickLeafMaterial(rng));
      }
    },
    flower: {
      none() {},
      blossom(mesh, position, direction, scale, config, rng) {
        const axis = normalize(direction);
        for (let i = 0; i < 5; i += 1) {
          const angle = (i / 5) * TWO_PI + rng() * 0.08;
          const petalDirection = rotateAroundAxis(safePerpendicular(axis), axis, angle);
          const center = add(position, scaleVec(petalDirection, scale * 0.34));
          const points = [
            { x: -0.28, y: -0.08 },
            { x: -0.08, y: 0.38 },
            { x: 0.12, y: 0.58 },
            { x: 0.3, y: 0.14 },
            { x: 0.18, y: -0.22 }
          ];
          addPlanarPart(mesh, center, add(axis, scaleVec(petalDirection, 0.35)), scale, points, "flower");
        }
        addLowPolySphere(mesh, position, scale * 0.16, "flowerCore", 6);
        mesh.meta.flowers += 1;
      },
      bell(mesh, position, direction, scale, config, rng) {
        const down = normalize(add(scaleVec(direction, 0.35), vec(0, -1, 0)));
        addCone(mesh, position, down, scale * 0.9, scale * 0.34, 8, "flower");
        mesh.meta.flowers += 1;
      },
      berry(mesh, position, direction, scale) {
        addLowPolySphere(mesh, position, scale * 0.36, "fruit", 8);
        mesh.meta.flowers += 1;
      },
      cone(mesh, position, direction, scale) {
        const down = normalize(add(scaleVec(direction, 0.2), vec(0, -1, 0)));
        addCone(mesh, position, down, scale * 1.15, scale * 0.32, 9, "cone");
        mesh.meta.flowers += 1;
      }
    }
  };

  function init() {
    bindControls();
    bindCanvas();
    applyConfigToInputs();
    scheduleGenerate();
    new ResizeObserver(() => scheduleRender()).observe(canvas);
  }

  function bindControls() {
    for (const input of inputs) {
      const eventName = input.type === "checkbox" ? "change" : "input";
      input.addEventListener(eventName, () => {
        const key = input.dataset.param;
        if (input.type === "checkbox") {
          state.config[key] = input.checked;
        } else if (input.type === "number" || input.type === "range") {
          state.config[key] = Number(input.value);
        } else {
          state.config[key] = input.value;
        }
        updateOutputs();
        scheduleGenerate();
      });
    }

    document.querySelectorAll("[data-preset]").forEach((button) => {
      button.addEventListener("click", () => {
        state.activePreset = button.dataset.preset;
        state.config = { ...PRESETS[state.activePreset] };
        state.camera.zoom = 1;
        applyConfigToInputs();
        updatePresetButtons();
        updateModuleButtons();
        scheduleGenerate();
      });
    });

    document.querySelectorAll("[data-module-type]").forEach((button) => {
      button.addEventListener("click", () => {
        state.config[button.dataset.moduleType] = button.dataset.module;
        updateModuleButtons();
        scheduleGenerate();
      });
    });

    document.querySelector("#randomizeSeed").addEventListener("click", () => {
      state.config.seed = Math.floor(1 + Math.random() * 999999);
      applyConfigToInputs();
      scheduleGenerate();
    });

    document.querySelectorAll("[data-export]").forEach((button) => {
      button.addEventListener("click", () => exportMesh(button.dataset.export));
    });
  }

  function bindCanvas() {
    canvas.addEventListener("pointerdown", (event) => {
      state.pointer.active = true;
      state.pointer.x = event.clientX;
      state.pointer.y = event.clientY;
      canvas.setPointerCapture(event.pointerId);
    });

    canvas.addEventListener("pointermove", (event) => {
      if (!state.pointer.active) {
        return;
      }
      const dx = event.clientX - state.pointer.x;
      const dy = event.clientY - state.pointer.y;
      state.pointer.x = event.clientX;
      state.pointer.y = event.clientY;
      state.camera.yaw += dx * 0.008;
      state.camera.pitch = clamp(state.camera.pitch + dy * 0.006, -1.08, 0.72);
      scheduleRender();
    });

    canvas.addEventListener("pointerup", (event) => {
      state.pointer.active = false;
      canvas.releasePointerCapture(event.pointerId);
    });

    canvas.addEventListener(
      "wheel",
      (event) => {
        event.preventDefault();
        state.camera.zoom = clamp(state.camera.zoom * (1 + event.deltaY * 0.001), 0.55, 2.8);
        scheduleRender();
      },
      { passive: false }
    );
  }

  function applyConfigToInputs() {
    for (const input of inputs) {
      const key = input.dataset.param;
      if (!(key in state.config)) {
        continue;
      }
      if (input.type === "checkbox") {
        input.checked = Boolean(state.config[key]);
      } else {
        input.value = state.config[key];
      }
    }
    updateOutputs();
    updatePresetButtons();
    updateModuleButtons();
  }

  function updateOutputs() {
    const c = state.config;
    setOutput("seed", String(c.seed));
    setOutput("height", `${c.height.toFixed(1)} m`);
    setOutput("trunkRadius", c.trunkRadius.toFixed(2));
    setOutput("trunkTaper", percent(c.trunkTaper));
    setOutput("trunkCurve", percent(c.trunkCurve));
    setOutput("branchLevels", String(c.branchLevels));
    setOutput("branchFrequency", percent(c.branchFrequency));
    setOutput("branchDensity", percent(c.branchDensity));
    setOutput("branchAngle", `${Math.round(c.branchAngle)}°`);
    setOutput("branchLength", percent(c.branchLength));
    setOutput("crownWidth", percent(c.crownWidth));
    setOutput("leafDensity", percent(c.leafDensity));
    setOutput("leafSize", c.leafSize.toFixed(2));
    setOutput("flowerDensity", percent(c.flowerDensity));
    setOutput("flowerSize", c.flowerSize.toFixed(2));
  }

  function setOutput(key, value) {
    const output = document.querySelector(`#${key}Value`);
    if (output) {
      output.value = value;
      output.textContent = value;
    }
  }

  function updatePresetButtons() {
    document.querySelectorAll("[data-preset]").forEach((button) => {
      button.classList.toggle("is-active", button.dataset.preset === state.activePreset);
    });
  }

  function updateModuleButtons() {
    document.querySelectorAll("[data-module-type]").forEach((button) => {
      const key = button.dataset.moduleType;
      button.classList.toggle("is-active", state.config[key] === button.dataset.module);
    });
  }

  function scheduleGenerate() {
    if (state.generateFrame) {
      cancelAnimationFrame(state.generateFrame);
    }
    state.generateFrame = requestAnimationFrame(() => {
      state.generateFrame = 0;
      state.mesh = generatePlant(state.config);
      updateStats();
      scheduleRender();
    });
  }

  function scheduleRender() {
    if (state.renderFrame) {
      return;
    }
    state.renderFrame = requestAnimationFrame(() => {
      state.renderFrame = 0;
      render();
    });
  }

  function generatePlant(config) {
    const rng = mulberry32(hashSeed(config.seed));
    const mesh = new Mesh(buildMaterials(config));
    const stemCount = config.style === "shrub" ? Math.round(3 + config.branchDensity * 5) : 1;

    for (let stemIndex = 0; stemIndex < stemCount; stemIndex += 1) {
      const stemAngle = (stemIndex / Math.max(1, stemCount)) * TWO_PI + rng() * 0.55;
      const stemSpread = config.style === "shrub" ? randRange(rng, 0.03, 0.22) * config.crownWidth : 0;
      const base = vec(Math.cos(stemAngle) * stemSpread, 0, Math.sin(stemAngle) * stemSpread);
      const stemHeight =
        config.height * (config.style === "shrub" ? randRange(rng, 0.72, 1.04) : randRange(rng, 0.96, 1.04));
      const stemRadius = config.trunkRadius * (config.style === "shrub" ? randRange(rng, 0.56, 0.86) : 1);
      const lean = config.style === "shrub" ? randRange(rng, 0.08, 0.22) : randRange(rng, 0.01, 0.08);
      const stem = buildStem(mesh, config, rng, base, stemHeight, stemRadius, stemAngle, lean);
      addBranches(mesh, config, rng, stem, stemHeight, stemRadius);
      addTerminalGrowth(
        mesh,
        config,
        rng,
        stem.path[stem.path.length - 1],
        normalize(sub(stem.path[stem.path.length - 1], stem.path[stem.path.length - 2]))
      );
    }

    computeBounds(mesh);
    return mesh;
  }

  function buildStem(mesh, config, rng, base, height, radius, leanYaw, lean) {
    const segments = config.style === "shrub" ? 6 : 10;
    const path = [];
    const curveYaw = rng() * TWO_PI;
    const leanDirection = vec(Math.cos(leanYaw), 0, Math.sin(leanYaw));
    const sideDirection = vec(Math.cos(curveYaw), 0, Math.sin(curveYaw));
    const curveStrength = config.trunkCurve * height * (config.style === "conifer" ? 0.035 : 0.07);

    for (let i = 0; i <= segments; i += 1) {
      const t = i / segments;
      const sway = Math.sin(t * Math.PI) * curveStrength * randRange(rng, 0.7, 1.15);
      path.push(
        add(
          add(base, scaleVec(leanDirection, height * lean * t)),
          add(vec(0, height * t, 0), scaleVec(sideDirection, sway))
        )
      );
    }

    const topRadius = Math.max(radius * 0.08, radius * (1 - config.trunkTaper * 0.86));
    const tube = addCurvedCylinder(mesh, path, radius, topRadius, config.style === "shrub" ? 7 : 10, "bark");
    return { path, tube };
  }

  function addBranches(mesh, config, rng, stem, height, trunkRadius) {
    const path = stem.path;
    const levelCount = Math.max(1, Math.round(config.branchLevels));
    const levelStart = config.style === "broadleaf" ? 0.28 : config.style === "conifer" ? 0.16 : 0.12;
    const levelEnd = config.style === "conifer" ? 0.95 : 0.9;

    for (let level = 0; level < levelCount; level += 1) {
      const levelRatio = levelCount === 1 ? 0.5 : level / (levelCount - 1);
      const stemT = lerp(levelStart, levelEnd, levelRatio);
      const origin = pointOnPath(path, stemT);
      const slotsBase = config.style === "conifer" ? 4 : 2;
      const slots = Math.round(slotsBase + config.branchDensity * (config.style === "conifer" ? 6 : 5));
      const whorlOffset = rng() * TWO_PI;

      for (let slot = 0; slot < slots; slot += 1) {
        if (rng() > config.branchFrequency) {
          continue;
        }
        const yaw = whorlOffset + (slot / slots) * TWO_PI + randRange(rng, -0.22, 0.22);
        const spread = config.branchAngle * DEG;
        let vertical = Math.cos(spread);
        const horizontal = Math.sin(spread);

        if (config.style === "conifer") {
          vertical -= lerp(0.22, -0.1, stemT);
        } else if (config.style === "shrub") {
          vertical += 0.12;
        }

        const direction = normalize(vec(Math.cos(yaw) * horizontal, vertical, Math.sin(yaw) * horizontal));
        const crownFactor =
          config.style === "conifer"
            ? Math.max(0.12, 1.12 - stemT)
            : config.style === "shrub"
              ? 0.76 + (1 - stemT) * 0.28
              : 0.32 + Math.sin(stemT * Math.PI) * 0.88;
        const length = height * 0.24 * config.branchLength * config.crownWidth * crownFactor * randRange(rng, 0.72, 1.14);
        const radius = trunkRadius * Math.pow(1 - stemT, 1.08) * randRange(rng, 0.34, 0.52);
        addBranch(mesh, config, rng, origin, direction, length, radius, 0, stemT, stem.tube, stemT);
      }
    }
  }

  function addBranch(mesh, config, rng, origin, direction, length, radius, depth, tierRatio, parentTube = null, parentT = 0) {
    if (length < 0.08 || radius < 0.008 || mesh.meta.branches > 560) {
      return;
    }

    mesh.meta.branches += 1;
    const steps = depth === 0 ? 4 : 3;
    const junctionBase = parentTube ? getJunctionBasePoint(mesh, parentTube, parentT, direction) : null;
    const branchOrigin = junctionBase || origin;
    const path = [branchOrigin];
    const bendSide = safePerpendicular(direction);
    const bendAmount = randRange(rng, -0.18, 0.18) * length * (0.45 + config.trunkCurve);
    const arch =
      config.style === "conifer"
        ? -length * (0.1 + 0.1 * (1 - tierRatio))
        : length * (0.06 + 0.08 * (1 - tierRatio));

    for (let i = 1; i <= steps; i += 1) {
      const t = i / steps;
      const basePoint = add(branchOrigin, scaleVec(direction, length * t));
      const bend = scaleVec(bendSide, Math.sin(t * Math.PI) * bendAmount);
      const lift = vec(0, Math.sin(t * Math.PI) * arch * (depth === 0 ? 1 : 0.55), 0);
      path.push(add(add(basePoint, bend), lift));
    }

    const branchTube = addCurvedCylinder(mesh, path, radius, Math.max(radius * 0.18, 0.006), depth === 0 ? 7 : 6, "bark", {
      capStart: !parentTube
    });
    if (parentTube) {
      addJunctionBridge(mesh, parentTube, parentT, branchTube, direction, "bark");
    }

    if (config.leafModule === "needle") {
      for (let i = 1; i < path.length; i += 1) {
        if (rng() < config.leafDensity * 0.84) {
          addFoliageCluster(mesh, config, rng, path[i], normalize(sub(path[i], path[i - 1])), length * 0.2, depth + 1);
        }
      }
    }

    const maxDepth = config.style === "conifer" ? 1 : 2;
    if (depth < maxDepth) {
      const splitBase = config.style === "shrub" ? 3 : 2;
      const splitCount = Math.round(config.branchDensity * splitBase + (depth === 0 ? 1 : 0));
      const parentYaw = Math.atan2(direction.z, direction.x);

      for (let i = 0; i < splitCount; i += 1) {
        if (rng() > config.branchFrequency * (0.92 - depth * 0.22)) {
          continue;
        }
        const t = randRange(rng, 0.38, 0.82);
        const start = pointOnPath(path, t);
        const sideAngle = parentYaw + randRange(rng, -1.25, 1.25) + (i - splitCount / 2) * 0.42;
        const sideDirection = vec(Math.cos(sideAngle), 0, Math.sin(sideAngle));
        const childDirection = normalize(
          add(
            add(scaleVec(direction, config.style === "conifer" ? 0.52 : 0.42), scaleVec(sideDirection, 0.9)),
            vec(0, config.style === "conifer" ? -0.06 : randRange(rng, 0.05, 0.28), 0)
          )
        );
        addBranch(
          mesh,
          config,
          rng,
          start,
          childDirection,
          length * randRange(rng, 0.36, 0.62) * (1 - depth * 0.13),
          radius * randRange(rng, 0.45, 0.6),
          depth + 1,
          tierRatio,
          branchTube,
          t
        );
      }
    }

    if (config.leafModule !== "needle") {
      addDistributedFoliage(mesh, config, rng, path, length, depth);
    }

    addFoliageCluster(mesh, config, rng, path[path.length - 1], direction, length, depth, {
      densityScale: config.leafModule === "needle" ? 1 : depth === 0 ? 0.5 : 0.66,
      sizeScale: config.leafModule === "needle" ? 1 : 0.9,
      spreadScale: config.leafModule === "needle" ? 1 : 1.08,
      flowerScale: config.leafModule === "needle" ? 1 : 0.72
    });
  }

  function addTerminalGrowth(mesh, config, rng, position, direction) {
    const count = config.style === "conifer" ? 9 : config.style === "shrub" ? 2 : 3;
    for (let i = 0; i < count; i += 1) {
      const dir = normalize(add(direction, randomConeVector(rng, 0.7)));
      addFoliageCluster(mesh, config, rng, add(position, scaleVec(dir, config.leafSize * 0.3)), dir, config.leafSize, 2, {
        densityScale: config.style === "conifer" ? 1 : 0.52,
        sizeScale: config.style === "conifer" ? 1 : 0.88,
        spreadScale: 1.1,
        flowerScale: 0.58
      });
    }
  }

  function addDistributedFoliage(mesh, config, rng, path, branchLength, depth) {
    const sampleCount = Math.round(
      lerp(1, config.style === "shrub" ? 5 : 4, config.leafDensity) * (depth === 0 ? 1.12 : 0.82)
    );
    const startT = depth === 0 ? (config.style === "shrub" ? 0.3 : 0.48) : 0.24;
    const endT = 0.9;

    for (let i = 0; i < sampleCount; i += 1) {
      const t = lerp(startT, endT, (i + randRange(rng, 0.18, 0.82)) / sampleCount);
      if (rng() > config.leafDensity * (config.style === "shrub" ? 0.98 : 0.84)) {
        continue;
      }

      const tangent = tangentAtPathRatio(path, t);
      const side = rotateAroundAxis(safePerpendicular(tangent), tangent, rng() * TWO_PI);
      const anchor = add(pointOnPath(path, t), scaleVec(side, config.leafSize * randRange(rng, 0.32, 0.95)));
      const outward = normalize(add(add(scaleVec(tangent, 0.34), scaleVec(side, 0.78)), vec(0, randRange(rng, 0.06, 0.28), 0)));

      addFoliageCluster(mesh, config, rng, anchor, outward, branchLength * 0.25, depth + 1, {
        densityScale: depth === 0 ? 0.36 : 0.48,
        sizeScale: depth === 0 ? 0.76 : 0.84,
        spreadScale: 0.72,
        flowerScale: 0.38
      });
    }
  }

  function addFoliageCluster(mesh, config, rng, anchor, direction, branchLength, depth, options = {}) {
    const leafFactory = partModules.leaf[config.leafModule] || partModules.leaf.oval;
    const densityScale = options.densityScale ?? 1;
    const sizeScale = options.sizeScale ?? 1;
    const spreadScale = options.spreadScale ?? 1;
    const flowerScale = options.flowerScale ?? densityScale;
    const effectiveLeafDensity = clamp(config.leafDensity * densityScale, 0, 1);
    const maxLeaves = config.leafModule === "needle" ? 4 + config.leafDensity * 8 : 3 + config.leafDensity * 12;
    const leafCount = Math.round(effectiveLeafDensity * maxLeaves);
    const clusterRadius = config.leafSize * sizeScale * spreadScale * (config.style === "shrub" ? 1.0 : 1.25) * (depth > 1 ? 0.8 : 1);

    for (let i = 0; i < leafCount; i += 1) {
      const outward = normalize(add(direction, randomConeVector(rng, 0.82)));
      const position = add(anchor, scaleVec(randomConeVector(rng, 1), clusterRadius * randRange(rng, 0.08, 0.58)));
      const scaleValue =
        config.leafSize *
        sizeScale *
        randRange(rng, 0.72, 1.22) *
        (config.leafModule === "needle" ? 0.72 : 1) *
        (config.style === "conifer" ? 0.9 : 1);
      leafFactory(mesh, position, outward, scaleValue, config, rng);
      if (config.leafModule !== "needle") {
        mesh.meta.leaves += 1;
      }
    }

    const flowerFactory = partModules.flower[config.flowerModule] || partModules.flower.none;
    if (config.flowerModule !== "none") {
      const flowerCount = Math.round(config.flowerDensity * flowerScale * Math.max(1, leafCount) * 0.42);
      for (let i = 0; i < flowerCount; i += 1) {
        const outward = normalize(add(direction, randomConeVector(rng, 0.92)));
        const position = add(
          anchor,
          add(scaleVec(outward, config.leafSize * randRange(rng, 0.25, 0.8)), scaleVec(randomConeVector(rng, 1), clusterRadius * 0.25))
        );
        flowerFactory(mesh, position, outward, config.flowerSize * sizeScale * randRange(rng, 0.78, 1.22), config, rng);
      }
    }
  }

  function buildMaterials(config) {
    return {
      bark: { name: "bark", color: config.barkColor },
      leaf: { name: "leaf", color: config.leafColor },
      leafAccent: { name: "leaf_accent", color: config.leafAccentColor },
      flower: { name: "flower", color: config.flowerColor },
      flowerCore: { name: "flower_core", color: "#d49b34" },
      fruit: { name: "fruit", color: config.flowerColor },
      cone: { name: "cone", color: config.flowerColor }
    };
  }

  function addCurvedCylinder(mesh, points, radiusStart, radiusEnd, sides, material, options = {}) {
    if (points.length < 2) {
      return { frames: [], material, path: points, rings: [], sides };
    }

    const capStart = options.capStart !== false;
    const capEnd = options.capEnd !== false;
    const frames = [];
    const rings = [];
    let carriedU = null;

    for (let pointIndex = 0; pointIndex < points.length; pointIndex += 1) {
      const t = pointIndex / (points.length - 1);
      const tangent = tangentAtPathIndex(points, pointIndex);
      let u = carriedU ? sub(carriedU, scaleVec(tangent, dot(carriedU, tangent))) : basisFromDirection(tangent).u;
      if (length(u) < 0.0001) {
        u = safePerpendicular(tangent);
      }
      u = normalize(u);
      const v = normalize(cross(u, tangent));
      carriedU = u;

      const radius = lerp(radiusStart, radiusEnd, t);
      frames.push({
        center: points[pointIndex],
        tangent,
        u,
        v,
        radius
      });
      const ring = [];
      for (let sideIndex = 0; sideIndex < sides; sideIndex += 1) {
        const angle = (sideIndex / sides) * TWO_PI;
        const offset = add(scaleVec(u, Math.cos(angle) * radius), scaleVec(v, Math.sin(angle) * radius));
        ring.push(mesh.addVertex(add(points[pointIndex], offset)));
      }
      rings.push(ring);
    }

    for (let ringIndex = 0; ringIndex < rings.length - 1; ringIndex += 1) {
      const current = rings[ringIndex];
      const nextRing = rings[ringIndex + 1];
      for (let sideIndex = 0; sideIndex < sides; sideIndex += 1) {
        const nextSide = (sideIndex + 1) % sides;
        mesh.addFace(current[sideIndex], nextRing[sideIndex], nextRing[nextSide], material);
        mesh.addFace(current[sideIndex], nextRing[nextSide], current[nextSide], material);
      }
    }

    const startRing = rings[0];
    const endRing = rings[rings.length - 1];

    if (capStart) {
      const startCenter = mesh.addVertex(points[0]);
      for (let sideIndex = 0; sideIndex < sides; sideIndex += 1) {
        const nextSide = (sideIndex + 1) % sides;
        mesh.addFace(startCenter, startRing[nextSide], startRing[sideIndex], material);
      }
    }

    if (capEnd) {
      const endCenter = mesh.addVertex(points[points.length - 1]);
      for (let sideIndex = 0; sideIndex < sides; sideIndex += 1) {
        const nextSide = (sideIndex + 1) % sides;
        mesh.addFace(endCenter, endRing[sideIndex], endRing[nextSide], material);
      }
    }

    return { frames, material, path: points, rings, sides };
  }

  function getJunctionBasePoint(mesh, parentTube, parentT, direction) {
    const sample = sampleTubeRing(parentTube, parentT);
    if (!sample) {
      return null;
    }

    const radial = projectDirectionOntoPlane(direction, sample.frame.tangent, sample.frame.u);
    const sideIndex = nearestRingSide(mesh, sample.ring, sample.frame, radial);
    return mesh.vertices[sample.ring[sideIndex]];
  }

  function addJunctionBridge(mesh, parentTube, parentT, childTube, childDirection, material) {
    const parent = sampleTubeRing(parentTube, parentT);
    const child = sampleTubeRing(childTube, 0);
    if (!parent || !child || parent.ring.length < 3 || child.ring.length < 3) {
      return;
    }

    const attachDirection = projectDirectionOntoPlane(sub(child.frame.center, parent.frame.center), parent.frame.tangent, childDirection);
    const parentSideIndex = nearestRingSide(mesh, parent.ring, parent.frame, attachDirection);
    const childBackDirection = projectDirectionOntoPlane(
      sub(parent.frame.center, child.frame.center),
      child.frame.tangent,
      scaleVec(childDirection, -1)
    );
    const childSideIndex = nearestRingSide(mesh, child.ring, child.frame, childBackDirection);
    let bridgeCount = Math.min(5, parent.ring.length, child.ring.length);
    if (bridgeCount % 2 === 0) {
      bridgeCount -= 1;
    }
    bridgeCount = Math.max(3, bridgeCount);

    const parentArc = ringArc(parent.ring, parentSideIndex, bridgeCount);
    let childArc = ringArc(child.ring, childSideIndex, bridgeCount);
    const reversedChildArc = [...childArc].reverse();
    if (arcDistance(mesh, parentArc, reversedChildArc) < arcDistance(mesh, parentArc, childArc)) {
      childArc = reversedChildArc;
    }

    for (let i = 0; i < bridgeCount - 1; i += 1) {
      mesh.addFace(parentArc[i], childArc[i], childArc[i + 1], material);
      mesh.addFace(parentArc[i], childArc[i + 1], parentArc[i + 1], material);
    }
    mesh.meta.junctions += 1;
  }

  function sampleTubeRing(tube, t) {
    if (!tube || !tube.rings.length || !tube.frames.length) {
      return null;
    }
    const ringIndex = clamp(Math.round(clamp(t, 0, 1) * (tube.rings.length - 1)), 0, tube.rings.length - 1);
    return {
      frame: tube.frames[ringIndex],
      ring: tube.rings[ringIndex],
      ringIndex
    };
  }

  function nearestRingSide(mesh, ring, frame, direction) {
    let bestIndex = 0;
    let bestScore = -Infinity;
    for (let i = 0; i < ring.length; i += 1) {
      const radial = normalize(sub(mesh.vertices[ring[i]], frame.center));
      const score = dot(radial, direction);
      if (score > bestScore) {
        bestScore = score;
        bestIndex = i;
      }
    }
    return bestIndex;
  }

  function ringArc(ring, centerIndex, count) {
    const arc = [];
    const half = Math.floor(count / 2);
    for (let offset = -half; offset <= half; offset += 1) {
      arc.push(ring[wrapIndex(centerIndex + offset, ring.length)]);
    }
    return arc;
  }

  function arcDistance(mesh, a, b) {
    let total = 0;
    for (let i = 0; i < Math.min(a.length, b.length); i += 1) {
      total += length(sub(mesh.vertices[a[i]], mesh.vertices[b[i]]));
    }
    return total;
  }

  function addPlanarPart(mesh, center, direction, scaleValue, points, material) {
    const axis = normalize(direction);
    let side = safePerpendicular(axis);
    let up = normalize(cross(side, axis));
    const rotation = Math.atan2(center.z, center.x) * 0.35 + center.y * 0.17;
    side = rotateAroundAxis(side, axis, rotation);
    up = rotateAroundAxis(up, axis, rotation);

    const centerIndex = mesh.addVertex(center);
    const ring = points.map((point) => {
      const position = add(center, add(scaleVec(side, point.x * scaleValue), scaleVec(up, point.y * scaleValue)));
      return mesh.addVertex(position);
    });

    for (let i = 0; i < ring.length; i += 1) {
      mesh.addFace(centerIndex, ring[i], ring[(i + 1) % ring.length], material);
    }
  }

  function addCone(mesh, center, direction, lengthValue, radius, sides, material) {
    const axis = normalize(direction);
    const basis = basisFromDirection(axis);
    const top = mesh.addVertex(center);
    const ringCenter = add(center, scaleVec(axis, lengthValue));
    const ring = [];

    for (let i = 0; i < sides; i += 1) {
      const angle = (i / sides) * TWO_PI;
      ring.push(mesh.addVertex(add(ringCenter, add(scaleVec(basis.u, Math.cos(angle) * radius), scaleVec(basis.v, Math.sin(angle) * radius)))));
    }

    for (let i = 0; i < sides; i += 1) {
      mesh.addFace(top, ring[i], ring[(i + 1) % sides], material);
    }
  }

  function addLowPolySphere(mesh, center, radius, material, sides) {
    const top = mesh.addVertex(add(center, vec(0, radius, 0)));
    const bottom = mesh.addVertex(add(center, vec(0, -radius, 0)));
    const ring = [];

    for (let i = 0; i < sides; i += 1) {
      const angle = (i / sides) * TWO_PI;
      ring.push(mesh.addVertex(add(center, vec(Math.cos(angle) * radius, 0, Math.sin(angle) * radius))));
    }

    for (let i = 0; i < sides; i += 1) {
      const next = (i + 1) % sides;
      mesh.addFace(top, ring[i], ring[next], material);
      mesh.addFace(bottom, ring[next], ring[i], material);
    }
  }

  function render() {
    resizeCanvas();
    if (!state.mesh) {
      return;
    }

    const width = canvas.width;
    const height = canvas.height;
    ctx.clearRect(0, 0, width, height);
    const gradient = ctx.createLinearGradient(0, 0, 0, height);
    gradient.addColorStop(0, "#fbfcf5");
    gradient.addColorStop(1, "#eaf0e3");
    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, width, height);

    const camera = createCamera(state.mesh, width, height);
    drawGround(camera, width, height);
    drawMesh(state.mesh, camera);
  }

  function createCamera(mesh, width, height) {
    const radius = Math.max(1, mesh.bounds.radius);
    return {
      width,
      height,
      center: mesh.bounds.center,
      yaw: state.camera.yaw,
      pitch: state.camera.pitch,
      distance: radius * (2.45 + state.camera.zoom * 1.42),
      focal: Math.min(width, height) * 1.05
    };
  }

  function drawMesh(mesh, camera) {
    const projected = mesh.vertices.map((vertex) => projectPoint(vertex, camera));
    const renderFaces = [];
    const light = normalize(vec(-0.38, 0.82, -0.42));

    for (const face of mesh.faces) {
      const pa = projected[face.a];
      const pb = projected[face.b];
      const pc = projected[face.c];
      if (!pa.visible || !pb.visible || !pc.visible) {
        continue;
      }
      const normal = normalize(cross(sub(pb.camera, pa.camera), sub(pc.camera, pa.camera)));
      const brightness = 0.5 + Math.abs(dot(normal, light)) * 0.44;
      renderFaces.push({
        points: [pa, pb, pc],
        depth: (pa.depth + pb.depth + pc.depth) / 3,
        color: shadeColor(mesh.materials[face.material]?.color || "#888888", brightness),
        material: face.material
      });
    }

    renderFaces.sort((a, b) => b.depth - a.depth);

    for (const face of renderFaces) {
      ctx.beginPath();
      ctx.moveTo(face.points[0].x, face.points[0].y);
      ctx.lineTo(face.points[1].x, face.points[1].y);
      ctx.lineTo(face.points[2].x, face.points[2].y);
      ctx.closePath();
      ctx.fillStyle = face.color;
      ctx.fill();
      if (state.config.wireframe) {
        ctx.strokeStyle = face.material === "bark" ? "rgba(72, 52, 38, 0.32)" : "rgba(36, 54, 32, 0.22)";
        ctx.lineWidth = Math.max(1, window.devicePixelRatio || 1);
        ctx.stroke();
      }
    }
  }

  function drawGround(camera, width, height) {
    const span = Math.max(3, state.mesh.bounds.radius * 1.15);
    const step = span / 5;
    ctx.save();
    ctx.strokeStyle = "rgba(93, 117, 79, 0.18)";
    ctx.lineWidth = Math.max(1, (window.devicePixelRatio || 1) * 0.75);

    for (let i = -5; i <= 5; i += 1) {
      const offset = i * step;
      drawProjectedLine(vec(-span, 0, offset), vec(span, 0, offset), camera);
      drawProjectedLine(vec(offset, 0, -span), vec(offset, 0, span), camera);
    }

    const origin = projectPoint(vec(0, 0, 0), camera);
    if (origin.visible) {
      ctx.fillStyle = "rgba(47, 116, 82, 0.26)";
      ctx.beginPath();
      ctx.arc(origin.x, origin.y, 4 * (window.devicePixelRatio || 1), 0, TWO_PI);
      ctx.fill();
    }
    ctx.restore();

    function drawProjectedLine(a, b, activeCamera) {
      const pa = projectPoint(a, activeCamera);
      const pb = projectPoint(b, activeCamera);
      if (!pa.visible || !pb.visible) {
        return;
      }
      ctx.beginPath();
      ctx.moveTo(pa.x, pa.y);
      ctx.lineTo(pb.x, pb.y);
      ctx.stroke();
    }
  }

  function projectPoint(point, camera) {
    const centered = sub(point, camera.center);
    const yawed = rotateY(centered, camera.yaw);
    const pitched = rotateX(yawed, camera.pitch);
    const depth = camera.distance + pitched.z;
    const visible = depth > 0.08;
    const scaleValue = camera.focal / Math.max(depth, 0.08);
    return {
      x: camera.width * 0.5 + pitched.x * scaleValue,
      y: camera.height * 0.56 - pitched.y * scaleValue,
      depth,
      visible,
      camera: pitched
    };
  }

  function resizeCanvas() {
    const rect = canvas.getBoundingClientRect();
    const dpr = Math.min(2, window.devicePixelRatio || 1);
    const width = Math.max(320, Math.round(rect.width * dpr));
    const height = Math.max(320, Math.round(rect.height * dpr));
    if (canvas.width !== width || canvas.height !== height) {
      canvas.width = width;
      canvas.height = height;
    }
  }

  function updateStats() {
    const mesh = state.mesh;
    meshStats.textContent = `${mesh.vertices.length.toLocaleString()} vertices · ${mesh.faces.length.toLocaleString()} faces · ${mesh.meta.branches.toLocaleString()} branches · ${mesh.meta.junctions.toLocaleString()} junctions · ${mesh.meta.leaves.toLocaleString()} leaves`;
  }

  function exportMesh(format) {
    if (!state.mesh) {
      return;
    }
    const basename = `floraforge-${state.config.style}-${state.config.seed}`;
    if (format === "obj") {
      downloadBlob(`${basename}.obj`, new Blob([serializeOBJ(state.mesh)], { type: "text/plain" }));
    } else if (format === "glb") {
      downloadBlob(`${basename}.glb`, serializeGLB(state.mesh));
    } else if (format === "fbx") {
      downloadBlob(`${basename}.fbx`, new Blob([serializeFBX(state.mesh)], { type: "text/plain" }));
    }
  }

  function serializeOBJ(mesh) {
    const lines = [
      "# FloraForge procedural plant mesh",
      `# vertices ${mesh.vertices.length}`,
      `# faces ${mesh.faces.length}`
    ];
    for (const [key, material] of Object.entries(mesh.materials)) {
      lines.push(`# material ${key} ${material.color}`);
    }
    for (const vertex of mesh.vertices) {
      lines.push(`v ${fixed(vertex.x)} ${fixed(vertex.y)} ${fixed(vertex.z)}`);
    }

    let activeMaterial = "";
    for (const face of mesh.faces) {
      const materialName = mesh.materials[face.material]?.name || face.material;
      if (activeMaterial !== materialName) {
        activeMaterial = materialName;
        lines.push(`usemtl ${activeMaterial}`);
      }
      lines.push(`f ${face.a + 1} ${face.b + 1} ${face.c + 1}`);
    }
    return `${lines.join("\n")}\n`;
  }

  function serializeGLB(mesh) {
    const encoder = new TextEncoder();
    const materials = Object.entries(mesh.materials).filter(([key]) => mesh.faces.some((face) => face.material === key));
    const positions = new Float32Array(mesh.vertices.length * 3);
    const min = vec(Infinity, Infinity, Infinity);
    const max = vec(-Infinity, -Infinity, -Infinity);

    mesh.vertices.forEach((vertex, index) => {
      positions[index * 3] = vertex.x;
      positions[index * 3 + 1] = vertex.y;
      positions[index * 3 + 2] = vertex.z;
      min.x = Math.min(min.x, vertex.x);
      min.y = Math.min(min.y, vertex.y);
      min.z = Math.min(min.z, vertex.z);
      max.x = Math.max(max.x, vertex.x);
      max.y = Math.max(max.y, vertex.y);
      max.z = Math.max(max.z, vertex.z);
    });

    const parts = [];
    let byteOffset = 0;

    function append(bytes) {
      const pad = (4 - (byteOffset % 4)) % 4;
      if (pad) {
        parts.push(new Uint8Array(pad));
        byteOffset += pad;
      }
      const start = byteOffset;
      parts.push(bytes);
      byteOffset += bytes.byteLength;
      return start;
    }

    const bufferViews = [];
    const accessors = [];
    const primitives = [];
    const positionOffset = append(new Uint8Array(positions.buffer));
    bufferViews.push({
      buffer: 0,
      byteOffset: positionOffset,
      byteLength: positions.byteLength,
      target: 34962
    });
    accessors.push({
      bufferView: 0,
      componentType: 5126,
      count: mesh.vertices.length,
      type: "VEC3",
      min: [min.x, min.y, min.z],
      max: [max.x, max.y, max.z]
    });

    const useUint32 = mesh.vertices.length > 65535;
    const IndexArray = useUint32 ? Uint32Array : Uint16Array;
    const componentType = useUint32 ? 5125 : 5123;

    materials.forEach(([key], materialIndex) => {
      const indices = [];
      for (const face of mesh.faces) {
        if (face.material === key) {
          indices.push(face.a, face.b, face.c);
        }
      }
      const indexArray = new IndexArray(indices);
      const indexOffset = append(new Uint8Array(indexArray.buffer));
      bufferViews.push({
        buffer: 0,
        byteOffset: indexOffset,
        byteLength: indexArray.byteLength,
        target: 34963
      });
      accessors.push({
        bufferView: bufferViews.length - 1,
        componentType,
        count: indexArray.length,
        type: "SCALAR"
      });
      primitives.push({
        attributes: { POSITION: 0 },
        indices: accessors.length - 1,
        material: materialIndex,
        mode: 4
      });
    });

    const binary = mergeBytes(parts, byteOffset);
    const gltf = {
      asset: {
        version: "2.0",
        generator: "FloraForge"
      },
      scene: 0,
      scenes: [{ nodes: [0] }],
      nodes: [{ mesh: 0, name: "FloraForge_Plant" }],
      meshes: [{ primitives }],
      materials: materials.map(([, material]) => {
        const color = hexToRgb(material.color);
        return {
          name: material.name,
          doubleSided: true,
          pbrMetallicRoughness: {
            baseColorFactor: [color.r / 255, color.g / 255, color.b / 255, 1],
            metallicFactor: 0,
            roughnessFactor: 0.86
          }
        };
      }),
      buffers: [{ byteLength: binary.byteLength }],
      bufferViews,
      accessors
    };

    const jsonBytes = encoder.encode(JSON.stringify(gltf));
    const jsonPaddedLength = align4(jsonBytes.byteLength);
    const binPaddedLength = align4(binary.byteLength);
    const totalLength = 12 + 8 + jsonPaddedLength + 8 + binPaddedLength;
    const glb = new ArrayBuffer(totalLength);
    const view = new DataView(glb);
    let offset = 0;
    view.setUint32(offset, 0x46546c67, true);
    offset += 4;
    view.setUint32(offset, 2, true);
    offset += 4;
    view.setUint32(offset, totalLength, true);
    offset += 4;
    view.setUint32(offset, jsonPaddedLength, true);
    offset += 4;
    view.setUint32(offset, 0x4e4f534a, true);
    offset += 4;
    new Uint8Array(glb, offset, jsonBytes.byteLength).set(jsonBytes);
    new Uint8Array(glb, offset + jsonBytes.byteLength, jsonPaddedLength - jsonBytes.byteLength).fill(0x20);
    offset += jsonPaddedLength;
    view.setUint32(offset, binPaddedLength, true);
    offset += 4;
    view.setUint32(offset, 0x004e4942, true);
    offset += 4;
    new Uint8Array(glb, offset, binary.byteLength).set(binary);
    return new Blob([glb], { type: "model/gltf-binary" });
  }

  function serializeFBX(mesh) {
    const materialKeys = Object.keys(mesh.materials).filter((key) => mesh.faces.some((face) => face.material === key));
    const materialIndex = new Map(materialKeys.map((key, index) => [key, index]));
    const vertices = mesh.vertices.flatMap((vertex) => [fixed(vertex.x), fixed(vertex.y), fixed(vertex.z)]).join(",");
    const indices = mesh.faces
      .flatMap((face) => [face.a, face.b, -(face.c + 1)])
      .join(",");
    const faceMaterials = mesh.faces.map((face) => materialIndex.get(face.material) ?? 0).join(",");
    const layerMaterials = materialKeys
      .map((key, index) => {
        const material = mesh.materials[key];
        const color = hexToRgb(material.color);
        return `    Material: ${2000 + index}, "Material::${material.name}", "" {
      DiffuseColor: ${fixed(color.r / 255)},${fixed(color.g / 255)},${fixed(color.b / 255)}
    }`;
      })
      .join("\n");
    const connections = materialKeys
      .map((key, index) => `    C: "OO",${2000 + index},1000`)
      .join("\n");

    return `; FBX 7.4.0 generated by FloraForge
FBXHeaderExtension:  {
  FBXHeaderVersion: 1003
  FBXVersion: 7400
}
Objects:  {
  Geometry: 1001, "Geometry::FloraForgeMesh", "Mesh" {
    Vertices: *${mesh.vertices.length * 3} {
      a: ${vertices}
    }
    PolygonVertexIndex: *${mesh.faces.length * 3} {
      a: ${indices}
    }
    LayerElementMaterial: 0 {
      Version: 101
      Name: ""
      MappingInformationType: "ByPolygon"
      ReferenceInformationType: "IndexToDirect"
      Materials: *${mesh.faces.length} {
        a: ${faceMaterials}
      }
    }
  }
  Model: 1000, "Model::FloraForge_Plant", "Mesh" {}
${layerMaterials}
}
Connections:  {
  C: "OO",1001,1000
${connections}
}
`;
  }

  function downloadBlob(filename, blob) {
    const anchor = document.createElement("a");
    const url = URL.createObjectURL(blob);
    anchor.href = url;
    anchor.download = filename;
    document.body.append(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1200);
  }

  function computeBounds(mesh) {
    const min = vec(Infinity, Infinity, Infinity);
    const max = vec(-Infinity, -Infinity, -Infinity);
    for (const vertex of mesh.vertices) {
      min.x = Math.min(min.x, vertex.x);
      min.y = Math.min(min.y, vertex.y);
      min.z = Math.min(min.z, vertex.z);
      max.x = Math.max(max.x, vertex.x);
      max.y = Math.max(max.y, vertex.y);
      max.z = Math.max(max.z, vertex.z);
    }
    const center = scaleVec(add(min, max), 0.5);
    let radius = 0;
    for (const vertex of mesh.vertices) {
      radius = Math.max(radius, length(sub(vertex, center)));
    }
    mesh.bounds = { min, max, center, radius: Math.max(1, radius) };
  }

  function pointOnPath(path, t) {
    const clamped = clamp(t, 0, 1);
    const f = clamped * (path.length - 1);
    const index = Math.min(path.length - 2, Math.floor(f));
    return mix(path[index], path[index + 1], f - index);
  }

  function tangentAtPathIndex(path, index) {
    const previous = path[Math.max(0, index - 1)];
    const next = path[Math.min(path.length - 1, index + 1)];
    const tangent = sub(next, previous);
    if (length(tangent) < 0.0001 && index > 0) {
      return normalize(sub(path[index], path[index - 1]));
    }
    return normalize(tangent);
  }

  function tangentAtPathRatio(path, t) {
    const delta = 1 / Math.max(8, path.length * 3);
    const before = pointOnPath(path, clamp(t - delta, 0, 1));
    const after = pointOnPath(path, clamp(t + delta, 0, 1));
    return normalize(sub(after, before));
  }

  function projectDirectionOntoPlane(direction, normal, fallback) {
    let projected = sub(direction, scaleVec(normal, dot(direction, normal)));
    if (length(projected) < 0.0001) {
      projected = sub(fallback, scaleVec(normal, dot(fallback, normal)));
    }
    if (length(projected) < 0.0001) {
      projected = safePerpendicular(normal);
    }
    return normalize(projected);
  }

  function basisFromDirection(direction) {
    const reference = Math.abs(direction.y) < 0.92 ? vec(0, 1, 0) : vec(1, 0, 0);
    const u = normalize(cross(direction, reference));
    const v = normalize(cross(u, direction));
    return { u, v };
  }

  function safePerpendicular(direction) {
    const reference = Math.abs(direction.y) < 0.92 ? vec(0, 1, 0) : vec(1, 0, 0);
    return normalize(cross(direction, reference));
  }

  function safeSideVector(direction, size) {
    return scaleVec(safePerpendicular(direction), size);
  }

  function randomConeVector(rng, strength) {
    const angle = rng() * TWO_PI;
    const y = randRange(rng, -0.45, 0.65) * strength;
    const radial = Math.sqrt(Math.max(0, 1 - y * y));
    return normalize(vec(Math.cos(angle) * radial * strength, y, Math.sin(angle) * radial * strength));
  }

  function pickLeafMaterial(rng) {
    return rng() > 0.72 ? "leafAccent" : "leaf";
  }

  function vec(x, y, z) {
    return { x, y, z };
  }

  function add(a, b) {
    return vec(a.x + b.x, a.y + b.y, a.z + b.z);
  }

  function sub(a, b) {
    return vec(a.x - b.x, a.y - b.y, a.z - b.z);
  }

  function scaleVec(a, scalar) {
    return vec(a.x * scalar, a.y * scalar, a.z * scalar);
  }

  function mix(a, b, t) {
    return add(scaleVec(a, 1 - t), scaleVec(b, t));
  }

  function dot(a, b) {
    return a.x * b.x + a.y * b.y + a.z * b.z;
  }

  function cross(a, b) {
    return vec(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
  }

  function length(a) {
    return Math.hypot(a.x, a.y, a.z);
  }

  function normalize(a) {
    const value = length(a);
    if (value < 0.000001) {
      return vec(0, 1, 0);
    }
    return scaleVec(a, 1 / value);
  }

  function rotateAroundAxis(vector, axis, angle) {
    const unitAxis = normalize(axis);
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    return add(
      add(scaleVec(vector, cos), scaleVec(cross(unitAxis, vector), sin)),
      scaleVec(unitAxis, dot(unitAxis, vector) * (1 - cos))
    );
  }

  function rotateY(point, angle) {
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    return vec(point.x * cos - point.z * sin, point.y, point.x * sin + point.z * cos);
  }

  function rotateX(point, angle) {
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    return vec(point.x, point.y * cos - point.z * sin, point.y * sin + point.z * cos);
  }

  function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
  }

  function lerp(a, b, t) {
    return a + (b - a) * t;
  }

  function randRange(rng, min, max) {
    return min + (max - min) * rng();
  }

  function percent(value) {
    return `${Math.round(value * 100)}%`;
  }

  function fixed(value) {
    return Number(value).toFixed(6).replace(/\.?0+$/, "");
  }

  function wrapIndex(index, lengthValue) {
    return ((index % lengthValue) + lengthValue) % lengthValue;
  }

  function align4(value) {
    return value + ((4 - (value % 4)) % 4);
  }

  function mergeBytes(parts, byteLength) {
    const bytes = new Uint8Array(byteLength);
    let offset = 0;
    for (const part of parts) {
      bytes.set(part, offset);
      offset += part.byteLength;
    }
    return bytes;
  }

  function shadeColor(hex, amount) {
    const color = hexToRgb(hex);
    return `rgb(${Math.round(clamp(color.r * amount, 0, 255))}, ${Math.round(clamp(color.g * amount, 0, 255))}, ${Math.round(clamp(color.b * amount, 0, 255))})`;
  }

  function hexToRgb(hex) {
    const normalized = hex.replace("#", "");
    const value = Number.parseInt(
      normalized.length === 3
        ? normalized
            .split("")
            .map((part) => part + part)
            .join("")
        : normalized,
      16
    );
    return {
      r: (value >> 16) & 255,
      g: (value >> 8) & 255,
      b: value & 255
    };
  }

  function hashSeed(seed) {
    let value = Number(seed) || 1;
    value ^= value << 13;
    value ^= value >>> 17;
    value ^= value << 5;
    return value >>> 0;
  }

  function mulberry32(seed) {
    return function next() {
      let t = (seed += 0x6d2b79f5);
      t = Math.imul(t ^ (t >>> 15), t | 1);
      t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  init();
})();
