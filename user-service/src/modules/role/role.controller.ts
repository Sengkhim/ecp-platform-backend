import {Controller, Get, Post, Body, Patch, Param, Query, HttpCode} from '@nestjs/common';
import {RoleQueryFilter, RoleService} from './role.service';
import { CreateRoleDto } from './dto/create-role.dto';
import {ApiTags} from "@nestjs/swagger";

@ApiTags('Roles')
@Controller('roles')
export class RoleController {
   
    constructor(private readonly roleService: RoleService) {}

    @Post()
    @HttpCode(201)
    create(@Body() body: CreateRoleDto) {
        return this.roleService.create(body);
    }

    @Get()
    @HttpCode(200)
    findAll(@Query() filter: RoleQueryFilter) {
        return this.roleService.findAll(filter);
    }

    @Get(':id')
    @HttpCode(200)
    findOne(@Param('id') id: string) {
        return this.roleService.findOne(id);
    }

    @Patch(':id')
    @HttpCode(201)
    update(
        @Param('id') id: string,
        @Body() body: CreateRoleDto
    ) {
        return this.roleService.update(id, body);
    }
}
