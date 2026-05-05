import {Controller, Post, Body, Param, HttpCode, Patch} from '@nestjs/common';
import {ApiTags} from "@nestjs/swagger";
import { UserRoleService } from '@/modules/user-role/user-role.service';
import { AssignRoleDto } from '@/modules/user-role/dto/assign-role.dto';

@ApiTags('User-Role')
@Controller('user-roles')
export class UserRoleController {
    constructor(private readonly userRoleService: UserRoleService) {}

    @Post()
    @HttpCode(201)
    create(@Body() request: AssignRoleDto) {
        return this.userRoleService.create(request);
    }

    @Patch(':userId/:roleId')
    @HttpCode(201)
    update(
        @Param('userId') userId: string,
        @Param('roleId') roleId: string,
        @Body() request: AssignRoleDto
    ) {
        return this.userRoleService.update(userId, roleId, request);
    }
}
